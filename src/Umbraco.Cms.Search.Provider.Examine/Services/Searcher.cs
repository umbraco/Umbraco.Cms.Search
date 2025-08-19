using System.Reflection;
using Examine;
using Examine.Lucene;
using Examine.Search;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Exceptions;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Searching;
using Umbraco.Cms.Search.Core.Models.Searching.Faceting;
using Umbraco.Cms.Search.Core.Models.Searching.Filtering;
using Umbraco.Cms.Search.Core.Models.Searching.Sorting;
using Umbraco.Cms.Search.Provider.Examine.Extensions;
using Umbraco.Cms.Search.Provider.Examine.Models.Searching.Filtering;
using Umbraco.Extensions;
using FacetResult = Umbraco.Cms.Search.Core.Models.Searching.Faceting.FacetResult;
using SearchResult = Umbraco.Cms.Search.Core.Models.Searching.SearchResult;

namespace Umbraco.Cms.Search.Provider.Examine.Services;

public class Searcher : IExamineSearcher
{
    private readonly IExamineManager _examineManager;

    public Searcher(IExamineManager examineManager)
        => _examineManager = examineManager;

    public Task<SearchResult> SearchAsync(string indexAlias, string? query, IEnumerable<Filter>? filters,
        IEnumerable<Facet>? facets, IEnumerable<Sorter>? sorters,
        string? culture, string? segment, AccessContext? accessContext, int skip, int take)
    {
        // Special case if no parameters are provided, return an empty list.
        if (query is null && filters is null && facets is null && sorters is null && culture is null &&
            segment is null && accessContext is null)
        {
            return Task.FromResult(new SearchResult(0, Array.Empty<Document>(), Array.Empty<FacetResult>()));
        }

        if (_examineManager.TryGetIndex(indexAlias, out IIndex? index) is false)
        {
            return Task.FromResult(new SearchResult(0, Array.Empty<Document>(), Array.Empty<FacetResult>()));
        }

        IBooleanOperation searchQuery = index.Searcher
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
                INestedBooleanOperation fieldQuery = nestedQuery.Field($"{Constants.Fields.FieldPrefix}_{Constants.Fields.TextsR1}",
                    transformedQuery.Boost(4));
                fieldQuery.Or().Field($"{Constants.Fields.FieldPrefix}_{Constants.Fields.TextsR2}",
                    transformedQuery.Boost(3));
                fieldQuery.Or().Field($"{Constants.Fields.FieldPrefix}_{Constants.Fields.TextsR3}",
                    transformedQuery.Boost(2));
                fieldQuery.Or().ManagedQuery(transformedQuery);
                fieldQuery.Or().Field($"{Constants.Fields.FieldPrefix}{Constants.Fields.AggregatedTexts}", transformedQuery.Escape());

                return fieldQuery;
            });
        }

        // Examine will overwrite subsequent facets of the same field, so make sure there aren't any duplicates.
        Facet[] deduplicateFacets = DeduplicateFacets(facets);

        Sorter[]? sortersAsArray = sorters as Sorter[] ?? sorters?.ToArray();

        // Add facets and filters
        AddFilters(searchQuery, filters);
        AddFacets(searchQuery, deduplicateFacets);
        AddSorters(searchQuery, sortersAsArray);
        AddProtection(searchQuery, accessContext);

        ISearchResults results;
        try
        {
            results = searchQuery.Execute(new QueryOptions(skip, take));
        }
        catch (ArgumentException e)
        {
            if (e.Message.Contains("field \"$facets\" was not indexed with SortedSetDocValues"))
            {
                throw new ConfigurationException("Tried querying a facet that did not exist, please configure your facets with FieldOptions.", e);
            }

            if (e.Message.Contains("dimension \"") && e.Message.Contains("\" was not indexed"))
            {
                throw new ConfigurationException("Tried querying a facet that did not exist, but the field did exist, please configure your facets with FieldOptions and set Facetable to true.", e);
            }

            throw;
        }

        IEnumerable<ISearchResult> searchResults;

        ScoreSorter? scoreSorter = sortersAsArray?.OfType<ScoreSorter>().FirstOrDefault();
        if (scoreSorter is not null)
        {
            searchResults = results.OrderBy(x => x.Score, scoreSorter.Direction);
        }
        else
        {
            searchResults = results;
        }

        FacetResult[] facetResults = facets is null ? [] : MapFacets(results, deduplicateFacets).ToArray();
        Document[] searchResultDocuments = searchResults.Select(MapToDocument).WhereNotNull().ToArray();

        return Task.FromResult(new SearchResult(results.TotalItemCount, searchResultDocuments, facetResults));
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
            var keys = $"{Guid.Empty.ToString().TransformDashes()} {accessContext.PrincipalId.ToString().TransformDashes()}";

            if (accessContext.GroupIds is not null)
            {
                foreach (Guid id in accessContext.GroupIds)
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
        foreach (Sorter sorter in sorters)
        {
            SortableField? sortableField = MapSorter(sorter);
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

        foreach (Filter filter in filters)
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
                            INestedBooleanOperation fieldQuery = nestedQuery.Field($"{Constants.Fields.FieldPrefix}{textFilter.FieldName}_{Constants.Fields.Texts}", textFilterValue.MultipleCharacterWildcard());
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
                            INestedBooleanOperation fieldQuery = nestedQuery.Field($"{Constants.Fields.FieldPrefix}{textFilter.FieldName}_{Constants.Fields.Texts}", textFilterValue);
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
                    searchQuery.AddRangeFilter(integerRangeFieldName, integerRangeFilter.Negate, integerRanges);
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
                    var doubleExactFilter = new DoubleExactFilter(filter.FieldName, decimalExactFilter.Values.Select(xx => (double)xx).ToArray(), filter.Negate);
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

        searchQuery.WithFacets(facetOperations =>
        {
            foreach (Facet facet in facets)
            {
                switch (facet)
                {
                    case IntegerExactFacet integerExactFacet:
                        facetOperations.FacetString($"{Constants.Fields.FieldPrefix}{integerExactFacet.FieldName}_{Constants.Fields.Integers}", config => config.MaxCount(100));
                        break;
                    case IntegerRangeFacet integerRangeFacet:
                        facetOperations.FacetLongRange(
                            $"{Constants.Fields.FieldPrefix}{integerRangeFacet.FieldName}_{Constants.Fields.Integers}",
                            integerRangeFacet.Ranges
                                .Select(x =>
                                    new Int64Range(x.Key, x.MinValue ?? 0, true, x.MaxValue ?? int.MaxValue, false))
                                .ToArray());
                        break;
                    case DecimalExactFacet decimalExactFacet:
                        facetOperations.FacetString($"{Constants.Fields.FieldPrefix}{decimalExactFacet.FieldName}_{Constants.Fields.Decimals}", config => config.MaxCount(100));
                        break;
                    case DecimalRangeFacet decimalRangeFacet:
                    {
                        DoubleRange[] doubleRanges = decimalRangeFacet.Ranges.Select(x =>
                                new DoubleRange(
                                    x.Key,
                                    decimal.ToDouble(x.MinValue ?? 0),
                                    true,
                                    decimal.ToDouble(x.MaxValue ?? 0),
                                    false))
                            .ToArray();
                        facetOperations.FacetDoubleRange(
                            $"{Constants.Fields.FieldPrefix}{facet.FieldName}_{Constants.Fields.Decimals}",
                            doubleRanges);
                        break;
                    }
                    case DateTimeOffsetExactFacet dateTimeOffsetExactFacet:
                        facetOperations.FacetString($"{Constants.Fields.FieldPrefix}{dateTimeOffsetExactFacet.FieldName}_{Constants.Fields.DateTimeOffsets}", config => config.MaxCount(100));
                        break;
                    case DateTimeOffsetRangeFacet dateTimeOffsetRangeFacet:
                        facetOperations.FacetLongRange(
                            $"{Constants.Fields.FieldPrefix}{dateTimeOffsetRangeFacet.FieldName}_{Constants.Fields.DateTimeOffsets}",
                            dateTimeOffsetRangeFacet.Ranges.Select(x => new Int64Range(
                                x.Key,
                                x.MinValue?.Ticks ?? DateTime.MinValue.Ticks, true,
                                x.MaxValue?.Ticks ?? DateTime.MaxValue.Ticks,
                                false))
                                .ToArray());
                        break;
                    case KeywordFacet keywordFacet:
                        var keywordFieldName = keywordFacet.FieldName.StartsWith(Constants.Fields.FieldPrefix)
                            ? $"{keywordFacet.FieldName}_{Constants.Fields.Keywords}"
                            : $"{Constants.Fields.FieldPrefix}{keywordFacet.FieldName}_{Constants.Fields.Keywords}";

                        facetOperations.FacetString(keywordFieldName, config => config.MaxCount(100));
                        break;

                }
            }
        });
    }

    private Facet[] DeduplicateFacets(IEnumerable<Facet>? facets)
    {
        if (facets is null)
        {
            return [];
        }

        return facets
            .GroupBy(f => (f.FieldName, f.GetType())) // group by field + facet type
            .Select(group =>
            {
                Facet first = group.First();

                return first.GetType().IsSubclassOf(typeof(RangeFacet<>)) ? MergeRangeFacets(group) : first;
            })
            .ToArray();
    }

    private Facet MergeRangeFacets(IEnumerable<Facet> facets)
    {
        Facet first = facets.First();
        Type type = first.GetType();

        PropertyInfo rangesProperty = type.GetProperty("Ranges")!;
        var allRanges = facets
            .SelectMany(f => (IEnumerable<object>)rangesProperty.GetValue(f)!)
            .Distinct()
            .ToArray();

        // Construct new facet with FieldName + merged ranges
        return (Facet)Activator.CreateInstance(type, first.FieldName, allRanges)!;
    }


    private static Document? MapToDocument(ISearchResult item)
    {
        var objectTypeString = item.Values.GetValueOrDefault($"__{Constants.Fields.IndexType}");

        Enum.TryParse(objectTypeString, out UmbracoObjectTypes umbracoObjectType);

        if (Guid.TryParse(item.Id, out Guid guidId))
        {
            return new Document(guidId, umbracoObjectType);
        }

        // The id of an item may be appended with _{culture_{segment}, so strip those and map to guid.
        var indexofUnderscore = item.Id.IndexOf('_');
        var idWithOutCulture = item.Id.Remove(indexofUnderscore);
        return Guid.TryParse(idWithOutCulture, out Guid idWithoutCultureGuid)
            ? new Document(idWithoutCultureGuid, umbracoObjectType)
            : null;
    }

    private IEnumerable<FacetResult> MapFacets(ISearchResults searchResults, IEnumerable<Facet> queryFacets)
    {
        foreach (Facet facet in queryFacets)
        {
            switch (facet)
            {
                case IntegerRangeFacet integerRangeFacet:
                {
                    IEnumerable<IntegerRangeFacetValue> integerRangeFacetResult = integerRangeFacet.Ranges.Select(x =>
                    {
                        int value = GetFacetCount($"{Constants.Fields.FieldPrefix}{facet.FieldName}_{Constants.Fields.Integers}", x.Key, searchResults);
                        return new IntegerRangeFacetValue(x.Key, x.MinValue, x.MaxValue, value);
                    });
                    yield return new FacetResult(facet.FieldName, integerRangeFacetResult);
                    break;
                }
                case IntegerExactFacet integerExactFacet:
                    IFacetResult? examineIntegerFacets = searchResults.GetFacet($"{Constants.Fields.FieldPrefix}{integerExactFacet.FieldName}_{Constants.Fields.Integers}");
                    if (examineIntegerFacets is null)
                    {
                        continue;
                    }

                    var integerExactFacetValues = new List<IntegerExactFacetValue>();
                    foreach (IFacetValue integerExactFacetValue in examineIntegerFacets)
                    {
                        if (int.TryParse(integerExactFacetValue.Label, out var labelValue) is false)
                        {
                            // Cannot convert the label to int, skipping.
                            continue;
                        }
                        integerExactFacetValues.Add(new IntegerExactFacetValue(labelValue, (int)integerExactFacetValue.Value));
                    }

                    yield return new FacetResult(facet.FieldName, integerExactFacetValues.OrderBy(x => x.Key));
                    break;
                case DecimalRangeFacet decimalRangeFacet:
                    IEnumerable<DecimalRangeFacetValue> decimalRangeFacetResult = decimalRangeFacet.Ranges.Select(x =>
                    {
                        int value = GetFacetCount($"{Constants.Fields.FieldPrefix}{facet.FieldName}_{Constants.Fields.Decimals}", x.Key, searchResults);
                        return new DecimalRangeFacetValue(x.Key, x.MinValue, x.MaxValue, value);
                    });
                    yield return new FacetResult(facet.FieldName, decimalRangeFacetResult);
                    break;
                case DecimalExactFacet decimalExactFacet:
                    IFacetResult? examineDecimalFacets = searchResults.GetFacet($"{Constants.Fields.FieldPrefix}{decimalExactFacet.FieldName}_{Constants.Fields.Decimals}");
                    if (examineDecimalFacets is null)
                    {
                        continue;
                    }

                    var decimalExactFacetValues = new List<DecimalExactFacetValue>();

                    foreach (IFacetValue decimalExactFacetValue in examineDecimalFacets)
                    {
                        if (decimal.TryParse(decimalExactFacetValue.Label, out var labelValue) is false)
                        {
                            // Cannot convert the label to decimal, skipping.
                            continue;
                        }
                        decimalExactFacetValues.Add(new DecimalExactFacetValue(labelValue, (int)decimalExactFacetValue.Value));
                    }

                    yield return new FacetResult(facet.FieldName, decimalExactFacetValues.OrderBy(x => x.Key));
                    break;
                 case DateTimeOffsetRangeFacet dateTimeOffsetRangeFacet:
                    IEnumerable<DateTimeOffsetRangeFacetValue> dateTimeOffsetRangeFacetResult = dateTimeOffsetRangeFacet.Ranges.Select(x =>
                    {
                        int value = GetFacetCount($"Umb_{facet.FieldName}_datetimeoffsets", x.Key, searchResults);
                        return new DateTimeOffsetRangeFacetValue(x.Key, x.MinValue, x.MaxValue, value);
                    });
                    yield return new FacetResult(facet.FieldName, dateTimeOffsetRangeFacetResult);
                    break;
                 case DateTimeOffsetExactFacet dateTimeOffsetExactFacet:
                    IFacetResult? examineDatetimeFacets = searchResults.GetFacet($"{Constants.Fields.FieldPrefix}{dateTimeOffsetExactFacet.FieldName}_{Constants.Fields.DateTimeOffsets}");
                    if (examineDatetimeFacets is null)
                    {
                        continue;
                    }

                    var datetimeOffsetExactFacetValues = new List<DateTimeOffsetExactFacetValue>();

                    foreach (IFacetValue datetimeExactFacetValue in examineDatetimeFacets)
                    {
                        if (long.TryParse(datetimeExactFacetValue.Label, out var ticks) is false)
                        {
                            // Cannot convert the label to ticks (long), skipping.
                            continue;
                        }
                        DateTimeOffset offSet = new DateTimeOffset().AddTicks(ticks);
                        datetimeOffsetExactFacetValues.Add(new DateTimeOffsetExactFacetValue(offSet, (int)datetimeExactFacetValue.Value));
                    }
                    yield return new FacetResult(facet.FieldName, datetimeOffsetExactFacetValues.OrderBy(x => x.Key));
                    break;
                case KeywordFacet keywordFacet:
                    IFacetResult? examineKeywordFacets = searchResults.GetFacet($"{Constants.Fields.FieldPrefix}{keywordFacet.FieldName}_{Constants.Fields.Keywords}");
                    if (examineKeywordFacets is null)
                    {
                        continue;
                    }

                    var keywordFacetValues = examineKeywordFacets.Select(examineKeywordFacet => new KeywordFacetValue(examineKeywordFacet.Label, (int)examineKeywordFacet.Value)).ToList();
                    yield return new FacetResult(facet.FieldName, keywordFacetValues);
                    break;
            }
        }
    }

    private static int GetFacetCount(string fieldName, string key, ISearchResults results)
        => (int?)results.GetFacet(fieldName)?.Facet(key)?.Value ?? 0;
}
