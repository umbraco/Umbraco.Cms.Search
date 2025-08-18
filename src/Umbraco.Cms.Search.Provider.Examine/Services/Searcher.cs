using Examine;
using Examine.Search;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Searching;
using Umbraco.Cms.Search.Core.Models.Searching.Faceting;
using Umbraco.Cms.Search.Core.Models.Searching.Filtering;
using Umbraco.Cms.Search.Core.Models.Searching.Sorting;
using Umbraco.Cms.Search.Provider.Examine.Extensions;
using Umbraco.Cms.Search.Provider.Examine.Mapping;
using Umbraco.Extensions;
using FacetResult = Umbraco.Cms.Search.Core.Models.Searching.Faceting.FacetResult;
using SearchResult = Umbraco.Cms.Search.Core.Models.Searching.SearchResult;

namespace Umbraco.Cms.Search.Provider.Examine.Services;

public class Searcher : IExamineSearcher
{
    private readonly IExamineManager _examineManager;
    private readonly IExamineMapper _examineMapper;

    public Searcher(IExamineManager examineManager, IExamineMapper examineMapper)
    {
        _examineManager = examineManager;
        _examineMapper = examineMapper;
    }

    public async Task<SearchResult> SearchAsync(string indexAlias, string? query, IEnumerable<Filter>? filters,
        IEnumerable<Facet>? facets, IEnumerable<Sorter>? sorters,
        string? culture, string? segment, AccessContext? accessContext, int skip, int take)
    {
        // Special case if no parameters are provided, return an empty list.
        if (query is null && filters is null && facets is null && sorters is null && culture is null &&
            segment is null && accessContext is null)
        {
            return await Task.FromResult(new SearchResult(0, Array.Empty<Document>(), Array.Empty<FacetResult>()));
        }

        if (_examineManager.TryGetIndex(indexAlias, out var index) is false)
        {
            return await Task.FromResult(new SearchResult(0, Array.Empty<Document>(), Array.Empty<FacetResult>()));
        }

        var searchQuery = index.Searcher
            .CreateQuery()
            .Field($"{Constants.Fields.FieldPrefix}{Constants.Fields.Culture}",
                $"{culture ?? "none"}".TransformDashes())
            .And()
            .Field($"{Constants.Fields.FieldPrefix}{Constants.Fields.Segment}",
                $"{segment ?? "none"}".TransformDashes());


        if (query is not null)
        {
            // This looks a little hacky, but managed query alone cannot handle some multicultural words, as the analyser is english based.
            // For example any japanese letters will not get a hit in the managed query.
            // We luckily can also query on the aggregated text field, to assure that these cases also gets included.
            searchQuery.And().Group(nestedQuery =>
            {
                var transformedQuery = query.TransformDashes();
                var fieldQuery = nestedQuery.Field($"{Constants.Fields.FieldPrefix}_{Constants.Fields.TextsR1}", transformedQuery.Boost(4));
                fieldQuery.Or().Field($"{Constants.Fields.FieldPrefix}_{Constants.Fields.TextsR2}", transformedQuery.Boost(3));
                fieldQuery.Or().Field($"{Constants.Fields.FieldPrefix}_{Constants.Fields.TextsR3}", transformedQuery.Boost(2));
                fieldQuery.Or().ManagedQuery(transformedQuery);
                fieldQuery.Or().Field($"{Constants.Fields.FieldPrefix}aggregated_texts", transformedQuery.Escape());

                return fieldQuery;
            });
        }

        // Add facets and filters
        AddFilters(searchQuery, filters);
        AddFacets(searchQuery, facets);
        AddSorters(searchQuery, sorters);
        AddProtection(searchQuery, accessContext);

        var results = searchQuery.Execute(new QueryOptions(skip, take));

        IEnumerable<ISearchResult> searchResults;

        var scoreSorters = sorters?.Select(x => x is ScoreSorter ? x : null).WhereNotNull();
        if (scoreSorters is not null && scoreSorters.Any())
        {
            searchResults = results.OrderBy(x => x.Score, scoreSorters.First().Direction);
        }
        else
        {
            searchResults = results;
        }

        return await Task.FromResult(new SearchResult(results.TotalItemCount,
            searchResults.Select(MapToDocument).WhereNotNull().ToArray(),
            facets is null ? Array.Empty<FacetResult>() : _examineMapper.MapFacets(results, facets)));
    }

    private void AddProtection(IBooleanOperation searchQuery, AccessContext? accessContext)
    {
        string protectionFieldName = $"{Constants.Fields.FieldPrefix}{Constants.Fields.Protection}";
        if (accessContext is null)
        {
            searchQuery.And().Field(protectionFieldName, Guid.Empty.ToString().TransformDashes());
        }
        else
        {
            var keys =
                $"{Guid.Empty.ToString().TransformDashes()} {accessContext.PrincipalId.ToString().TransformDashes()}";

            if (accessContext.GroupIds is not null)
            {
                foreach (var id in accessContext.GroupIds)
                {
                    keys += $" {id.ToString().TransformDashes()}";
                }
            }

            searchQuery.And().Field(protectionFieldName, keys);
        }
    }

    private void AddSorters(IBooleanOperation searchQuery, IEnumerable<Sorter>? sorters)
    {
        if (sorters is null)
        {
            return;
        }

        // TODO: Handling of multiple sorters, does this hold up?
        foreach (var sorter in sorters)
        {
            var sortableField = MapSorter(sorter);
            if (sortableField is null)
            {
                continue;
            }

            if (sorter.Direction is Direction.Ascending)
            {
                searchQuery.OrderBy(sortableField.Value);
            }
            else
            {
                searchQuery.OrderByDescending(sortableField.Value);
            }
        }
    }

    private SortableField? MapSorter(Sorter sorter)
    {
        var fieldNamePrefix = sorter.FieldName.StartsWith(Constants.Fields.FieldPrefix)
            ? $"{sorter.FieldName}"
            : $"{Constants.Fields.FieldPrefix}{sorter.FieldName}";

        return sorter switch
        {
            IntegerSorter => new SortableField($"{fieldNamePrefix}_{Constants.Fields.Integers}", SortType.Int),
            DecimalSorter => new SortableField($"{fieldNamePrefix}_{Constants.Fields.Decimals}", SortType.Double),
            DateTimeOffsetSorter => new SortableField($"{fieldNamePrefix}_{Constants.Fields.DateTimeOffsets}",
                SortType.Long),
            KeywordSorter => new SortableField($"{fieldNamePrefix}_{Constants.Fields.Keywords}", SortType.String),
            TextSorter => new SortableField($"{fieldNamePrefix}_{Constants.Fields.Texts}", SortType.String),
            ScoreSorter => null,
            _ => throw new NotSupportedException()
        };
    }

    private void AddFilters(IBooleanOperation searchQuery, IEnumerable<Filter>? filters)
    {
        if (filters is null)
        {
            return;
        }

        foreach (var filter in filters)
        {
            switch (filter)
            {
                case KeywordFilter keywordFilter:
                    var keywordFilterValue = string.Join(" ", keywordFilter.Values).TransformDashes();
                    var keywordFieldName = keywordFilter.FieldName.StartsWith(Constants.Fields.FieldPrefix)
                        ? $"{keywordFilter.FieldName}_{Constants.Fields.Keywords}"
                        : $"{Constants.Fields.FieldPrefix}{keywordFilter.FieldName}_{Constants.Fields.Keywords}";
                    if (keywordFilter.Negate)
                    {
                        searchQuery.Not().Field(keywordFieldName, keywordFilterValue);
                    }
                    else
                    {
                        searchQuery.And().Field(keywordFieldName, string.Join(" ", keywordFilterValue));
                    }

                    break;
                case TextFilter textFilter:
                    if (textFilter.Negate)
                    {
                        var negatedValue = string.Join(" ", textFilter.Values).TransformDashes();
                        searchQuery.Not().Field($"{Constants.Fields.FieldPrefix}{textFilter.FieldName}_{Constants.Fields.Texts}", negatedValue);
                        searchQuery.Not().Field($"{Constants.Fields.FieldPrefix}{textFilter.FieldName}_{Constants.Fields.TextsR1}", negatedValue);
                        searchQuery.Not().Field($"{Constants.Fields.FieldPrefix}{textFilter.FieldName}_{Constants.Fields.TextsR2}", negatedValue);
                        searchQuery.Not().Field($"{Constants.Fields.FieldPrefix}{textFilter.FieldName}_{Constants.Fields.TextsR3}", negatedValue);
                        continue;
                    }

                    // We only want to do wildcard searches IF there is just one word for now, as we can't do wildcards when joining.
                    if (textFilter.Values.Length == 1)
                    {
                        searchQuery.And().Group(nestedQuery =>
                        {
                            var textFilterValue = string.Join(" ", textFilter.Values).TransformDashes();
                            var fieldQuery = nestedQuery.Field($"{Constants.Fields.FieldPrefix}{textFilter.FieldName}_{Constants.Fields.Texts}", textFilterValue.MultipleCharacterWildcard());
                            fieldQuery.Or().Field($"{Constants.Fields.FieldPrefix}{textFilter.FieldName}_{Constants.Fields.TextsR1}", textFilterValue.MultipleCharacterWildcard());
                            fieldQuery.Or().Field($"{Constants.Fields.FieldPrefix}{textFilter.FieldName}_{Constants.Fields.TextsR2}", textFilterValue.MultipleCharacterWildcard());
                            return fieldQuery.Or().Field($"{Constants.Fields.FieldPrefix}{textFilter.FieldName}_{Constants.Fields.TextsR3}", textFilterValue.MultipleCharacterWildcard());
                        });
                    }
                    else
                    {
                        searchQuery.And().Group(nestedQuery =>
                        {
                            var textFilterValue = string.Join(" ", textFilter.Values).TransformDashes();
                            var fieldQuery = nestedQuery.Field($"{Constants.Fields.FieldPrefix}{textFilter.FieldName}_{Constants.Fields.Texts}", textFilterValue);
                            fieldQuery.Or().Field($"{Constants.Fields.FieldPrefix}{textFilter.FieldName}_{Constants.Fields.TextsR1}", textFilterValue);
                            fieldQuery.Or().Field($"{Constants.Fields.FieldPrefix}{textFilter.FieldName}_{Constants.Fields.TextsR2}", textFilterValue);
                            return fieldQuery.Or().Field($"{Constants.Fields.FieldPrefix}{textFilter.FieldName}_{Constants.Fields.TextsR3}", textFilterValue);
                        });
                    }

                    break;
                case IntegerRangeFilter integerRangeFilter:
                    var integerRangeFieldName =
                        $"{Constants.Fields.FieldPrefix}{integerRangeFilter.FieldName}_{Constants.Fields.Integers}";
                    FilterRange<int>[] integerRanges = integerRangeFilter.Ranges
                        .Select(r => new FilterRange<int>(r.MinValue ?? int.MinValue, r.MaxValue ?? int.MaxValue))
                        .ToArray();
                    searchQuery.AddRangeFilter<int>(integerRangeFieldName, integerRangeFilter.Negate, integerRanges);
                    break;
                case IntegerExactFilter integerExactFilter:
                    var integerExactFieldName =
                        $"{Constants.Fields.FieldPrefix}{integerExactFilter.FieldName}_{Constants.Fields.Integers}";
                    searchQuery.AddExactFilter(integerExactFieldName, integerExactFilter);
                    break;
                case DecimalRangeFilter decimalRangeFilter:
                    var decimalRangeFieldName =
                        $"{Constants.Fields.FieldPrefix}{decimalRangeFilter.FieldName}_{Constants.Fields.Decimals}";
                    FilterRange<double>[] doubleRanges = decimalRangeFilter.Ranges
                        .Select(r => new FilterRange<double>(Convert.ToDouble(r.MinValue ?? decimal.MinValue), Convert.ToDouble(r.MaxValue ?? decimal.MaxValue)))
                        .ToArray();
                    searchQuery.AddRangeFilter(decimalRangeFieldName, decimalRangeFilter.Negate, doubleRanges);
                    break;
                case DecimalExactFilter decimalExactFilter:
                    var decimalExactFieldName =
                        $"{Constants.Fields.FieldPrefix}{decimalExactFilter.FieldName}_{Constants.Fields.Decimals}";
                    var doubleExactFilter = new DoubleExactFilter(filter.FieldName, decimalExactFilter.Values.Select(Convert.ToDouble).ToArray(), filter.Negate);
                    searchQuery.AddExactFilter(decimalExactFieldName, doubleExactFilter);
                    break;
                case DateTimeOffsetRangeFilter dateTimeOffsetRangeFilter:
                    var dateTimeOffsetRangeFieldName =
                        $"{Constants.Fields.FieldPrefix}{dateTimeOffsetRangeFilter.FieldName}_{Constants.Fields.DateTimeOffsets}";
                    FilterRange<DateTime>[] dateTimeRanges = dateTimeOffsetRangeFilter.Ranges
                        .Select(r => new FilterRange<DateTime>(r.MinValue?.DateTime ?? DateTime.MinValue, r.MaxValue?.DateTime ?? DateTime.MaxValue))
                        .ToArray();
                    searchQuery.AddRangeFilter(dateTimeOffsetRangeFieldName, dateTimeOffsetRangeFilter.Negate, dateTimeRanges);
                    break;
                case DateTimeOffsetExactFilter dateTimeOffsetExactFilter:
                    var dateTimeOffsetExactFieldName =
                        $"{Constants.Fields.FieldPrefix}{dateTimeOffsetExactFilter.FieldName}_{Constants.Fields.DateTimeOffsets}";
                    var datetimeExactFilter = new DateTimeExactFilter(filter.FieldName, dateTimeOffsetExactFilter.Values.Select(value => value.DateTime).ToArray(), filter.Negate);
                    searchQuery.AddExactFilter(dateTimeOffsetExactFieldName, datetimeExactFilter);
                    break;
            }
        }
    }

    private void AddFacets(IOrdering searchQuery, IEnumerable<Facet>? facets)
    {
        if (facets is null)
        {
            return;
        }

        foreach (var facet in facets)
        {
            switch (facet)
            {
                case DateTimeOffsetRangeFacet dateTimeOffsetRangeFacet:
                    searchQuery.WithFacets(facetOperations => facetOperations.FacetLongRange(
                        $"{Constants.Fields.FieldPrefix}{dateTimeOffsetRangeFacet.FieldName}_{Constants.Fields.DateTimeOffsets}",
                        dateTimeOffsetRangeFacet.Ranges.Select(x => new Int64Range(x.Key,
                            x.MinValue?.Ticks ?? DateTime.MinValue.Ticks, true, x.MaxValue?.Ticks ?? DateTime.MaxValue.Ticks,
                            false)).ToArray()));
                    break;
                case DecimalExactFacet decimalExactFacet:
                    searchQuery.WithFacets(facetOperations =>
                        facetOperations.FacetString(
                            $"{Constants.Fields.FieldPrefix}{decimalExactFacet.FieldName}_{Constants.Fields.Decimals}"));
                    break;
                case IntegerRangeFacet integerRangeFacet:
                    searchQuery.WithFacets(facetOperations => facetOperations.FacetLongRange(
                        $"{Constants.Fields.FieldPrefix}{integerRangeFacet.FieldName}_{Constants.Fields.Integers}",
                        integerRangeFacet.Ranges
                            .Select(x => new Int64Range(x.Key, x.MinValue ?? 0, true, x.MaxValue ?? int.MaxValue, false))
                            .ToArray()));
                    break;
                case KeywordFacet keywordFacet:
                    searchQuery.WithFacets(facetOperations =>
                        facetOperations.FacetString(
                            $"{Constants.Fields.FieldPrefix}{keywordFacet.FieldName}_{Constants.Fields.Keywords}"));
                    break;
                case IntegerExactFacet integerExactFacet:
                    searchQuery.WithFacets(facetOperations =>
                        facetOperations.FacetString(
                            $"{Constants.Fields.FieldPrefix}{integerExactFacet.FieldName}_{Constants.Fields.Integers}"));
                    break;
                case DecimalRangeFacet decimalRangeFacet:
                {
                    var doubleRanges = decimalRangeFacet.Ranges.Select(x =>
                            new DoubleRange(x.Key, decimal.ToDouble(x.MinValue ?? 0), true, decimal.ToDouble(x.MaxValue ?? 0),
                                false))
                        .ToArray();
                    searchQuery.WithFacets(facetOperations =>
                        facetOperations.FacetDoubleRange(
                            $"{Constants.Fields.FieldPrefix}{facet.FieldName}_{Constants.Fields.Decimals}",
                            doubleRanges));
                    break;
                }
                case DateTimeOffsetExactFacet dateTimeOffsetExactFacet:
                    searchQuery.WithFacets(facetOperations =>
                        facetOperations.FacetString(
                            $"{Constants.Fields.FieldPrefix}{dateTimeOffsetExactFacet.FieldName}_{Constants.Fields.DateTimeOffsets}"));
                    break;
            }
        }
    }

    private static Document? MapToDocument(ISearchResult item)
    {
        var objectTypeString = item.Values.GetValueOrDefault("__IndexType");

        Enum.TryParse(objectTypeString, out UmbracoObjectTypes umbracoObjectType);

        if (Guid.TryParse(item.Id, out var guidId))
        {
            return new Document(guidId, umbracoObjectType);
        }

        // The id of an item may be appended with _{culture_{segment}, so strip those and map to guid.
        var indexofUnderscore = item.Id.IndexOf('_');
        var idWithOutCulture = item.Id.Remove(indexofUnderscore);
        return Guid.TryParse(idWithOutCulture, out var idWithoutCultureGuid)
            ? new Document(idWithoutCultureGuid, umbracoObjectType)
            : null;
    }
}
