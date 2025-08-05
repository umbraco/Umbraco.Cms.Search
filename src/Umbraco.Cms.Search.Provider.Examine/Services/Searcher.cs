using System.Globalization;
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
using ISearcher = Umbraco.Cms.Search.Core.Services.ISearcher;
using SearchResult = Umbraco.Cms.Search.Core.Models.Searching.SearchResult;

namespace Umbraco.Cms.Search.Provider.Examine.Services;

public class Searcher : ISearcher
{
    private readonly IExamineManager _examineManager;
    private readonly IExamineMapper _examineMapper;

    public Searcher(IExamineManager examineManager, IExamineMapper examineMapper)
    {
        _examineManager = examineManager;
        _examineMapper = examineMapper;
    }
    
    public async Task<SearchResult> SearchAsync(string indexAlias, string? query, IEnumerable<Filter>? filters, IEnumerable<Facet>? facets, IEnumerable<Sorter>? sorters,
        string? culture, string? segment, AccessContext? accessContext, int skip, int take)
    {
        // Special case if no parameters are provided, return an empty list.
        if (query is null && filters is null && facets is null && sorters is null && culture is null && segment is null && accessContext is null)
        {
            return await Task.FromResult(new SearchResult(0, Array.Empty<Document>(), Array.Empty<FacetResult>()));
        }
        if (_examineManager.TryGetIndex(indexAlias, out var index) is false)
        {
            return await Task.FromResult(new SearchResult(0, Array.Empty<Document>(), Array.Empty<FacetResult>()));
        }

        var searchQuery = index.Searcher
            .CreateQuery()
            .Field($"{Constants.Fields.FieldPrefix}{Constants.Fields.Culture}", $"{culture ?? "none"}".TransformDashes())
            .And()
            .Field($"{Constants.Fields.FieldPrefix}{Constants.Fields.Segment}", $"{segment ?? "none"}".TransformDashes());

        
        if (query is not null)
        {
            // This looks a little hacky, but managed query alone cannot handle some multicultural words, as the analyser is english based.
            // For example any japanese letters will not get a hit in the managed query.
            // We luckily can also query on the aggregated text field, to assure that these cases also gets included.
            searchQuery.And().Group(nestedQuery =>
            {
                var fieldQuery = nestedQuery.Field($"{Constants.Fields.FieldPrefix}aggregated_texts", query);
                fieldQuery.Or().ManagedQuery(query);
                return fieldQuery;
            });
        }
        
        // Add facets and filters
        AddFilters(searchQuery, filters);
        AddFacets(searchQuery, facets);
        AddSorters(searchQuery, sorters);
        AddProtection(searchQuery, accessContext);
        
        var results = searchQuery.Execute(new QueryOptions(skip, take));

        if (sorters is not null)
        {
            // TODO: Fix this hacky sorting, but examine does not handle sorting Guids as string well, so we have to manually do it.
            foreach (var sorter in sorters)
            {
                if (sorter is KeywordSorter keywordSorter)
                {
                    var sorted = SortByKey(results, $"{keywordSorter.FieldName}_{Constants.Fields.Keywords}", keywordSorter.Direction);
                    return await Task.FromResult(new SearchResult(results.TotalItemCount, sorted.Select(MapToDocument).WhereNotNull().ToArray(), facets is null ? Array.Empty<FacetResult>() : _examineMapper.MapFacets(results, facets)));
                }
            }
        }
        
        return await Task.FromResult(new SearchResult(results.TotalItemCount, results.Select(MapToDocument).WhereNotNull().ToArray(), facets is null ? Array.Empty<FacetResult>() : _examineMapper.MapFacets(results, facets)));
    }


    
    private IEnumerable<ISearchResult> SortByKey(
        ISearchResults results,
        string key,
        Direction direction)
    {
        return results.OrderBy(r => GetValueAsString(r.Values, key), direction).ToList();
    }

    private static string GetValueAsString(IReadOnlyDictionary<string, string> dict, string key)
    {
        return dict.TryGetValue(key, out var value) ? value : string.Empty; // Or null if preferred
    }
    
    private void AddProtection(IBooleanOperation searchQuery, AccessContext? accessContext)
    {
        string protectionFieldName = $"{Constants.Fields.FieldPrefix}{Constants.Fields.Protection}";
        if (accessContext is null)
        {
            searchQuery.And().Field(protectionFieldName, Guid.Empty.ToString());
        }
        else
        {
            var keys = $"{Guid.Empty} {accessContext.PrincipalId}";
            
            if (accessContext.GroupIds is not null)
            {
                foreach (var id in  accessContext.GroupIds.Select(x => x.ToString()))
                {
                    keys += string.Join(" ", id);
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
            if (sorter.Direction is Direction.Ascending)
            {
                searchQuery.OrderBy(sortableField);
            }
            else
            {
                searchQuery.OrderByDescending(sortableField);
            }
        }
    }

    private SortableField MapSorter(Sorter sorter)
    {
        var fieldNamePrefix = sorter.FieldName.StartsWith(Constants.Fields.FieldPrefix) ? $"{sorter.FieldName}" : $"{Constants.Fields.FieldPrefix}{sorter.FieldName}";

        return sorter switch
        {
            IntegerSorter => new SortableField($"{fieldNamePrefix}_{Constants.Fields.Integers}", SortType.Int),
            DecimalSorter => new SortableField($"{fieldNamePrefix}_{Constants.Fields.Decimals}", SortType.Double),
            DateTimeOffsetSorter => new SortableField($"{fieldNamePrefix}_{Constants.Fields.DateTimeOffsets}", SortType.Long),
            KeywordSorter => new SortableField($"{fieldNamePrefix}_{Constants.Fields.Keywords}", SortType.String),
            TextSorter => new SortableField($"{fieldNamePrefix}_{Constants.Fields.Texts}", SortType.String),
            ScoreSorter => new SortableField("", SortType.Score),
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
                    var keywordFilterValue = string.Join(" ", keywordFilter.Values);
                    var keywordFieldName = keywordFilter.FieldName.StartsWith(Constants.Fields.FieldPrefix) ? $"{keywordFilter.FieldName}_{Constants.Fields.Keywords}" : $"{Constants.Fields.FieldPrefix}{keywordFilter.FieldName}_{Constants.Fields.Keywords}";
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
                    var textFilterValue = string.Join(" ", textFilter.Values);
                    if (textFilter.Negate)
                    {
                        searchQuery.Not().Field($"{Constants.Fields.FieldPrefix}{textFilter.FieldName}_{Constants.Fields.Texts}", textFilterValue);
                        searchQuery.Not().Field($"{Constants.Fields.FieldPrefix}{textFilter.FieldName}_{Constants.Fields.TextsR1}", textFilterValue);
                        searchQuery.Not().Field($"{Constants.Fields.FieldPrefix}{textFilter.FieldName}_{Constants.Fields.TextsR2}", textFilterValue);
                        searchQuery.Not().Field($"{Constants.Fields.FieldPrefix}{textFilter.FieldName}_{Constants.Fields.TextsR3}", textFilterValue);
                    }
                    else
                    {
                        searchQuery.And().Group(nestedQuery =>
                        {
                            var fieldQuery = nestedQuery.Field($"{Constants.Fields.FieldPrefix}{textFilter.FieldName}_{Constants.Fields.Texts}", textFilterValue);
                            fieldQuery.Or().Field($"{Constants.Fields.FieldPrefix}{textFilter.FieldName}_{Constants.Fields.TextsR1}", textFilterValue.Boost(30));
                            fieldQuery.Or().Field($"{Constants.Fields.FieldPrefix}{textFilter.FieldName}_{Constants.Fields.TextsR2}", textFilterValue.Boost(20));
                            return fieldQuery.Or().Field($"{Constants.Fields.FieldPrefix}{textFilter.FieldName}_{Constants.Fields.TextsR3}", textFilterValue.Boost(10));
                        });
                    }
                    break;
                case IntegerRangeFilter integerRangeFilter:
                    var integerRangeFieldName = $"{Constants.Fields.FieldPrefix}{integerRangeFilter.FieldName}_{Constants.Fields.Integers}";
                    searchQuery.AddRangeFilter(integerRangeFieldName, integerRangeFilter.ToNonNullableRangeFilter());
                    break;
                case IntegerExactFilter integerExactFilter:
                    var integerExactFieldName = $"{Constants.Fields.FieldPrefix}{integerExactFilter.FieldName}_{Constants.Fields.Integers}";
                    searchQuery.AddExactFilter(integerExactFieldName, integerExactFilter);
                    break;
                case DecimalRangeFilter decimalRangeFilter:
                    var decimalRangeFieldName = $"{Constants.Fields.FieldPrefix}{decimalRangeFilter.FieldName}_{Constants.Fields.Decimals}";
                    searchQuery.AddRangeFilter(decimalRangeFieldName, decimalRangeFilter.ToNonNullableRangeFilter());
                    break;
                case DecimalExactFilter decimalExactFilter:
                    var decimalExactFieldName = $"{Constants.Fields.FieldPrefix}{decimalExactFilter.FieldName}_{Constants.Fields.Decimals}";
                    searchQuery.AddExactFilter(decimalExactFieldName, decimalExactFilter.ToDoubleExactFilter());
                    break;
                case DateTimeOffsetRangeFilter dateTimeOffsetRangeFilter:
                    var dateTimeOffsetRangeFieldName = $"{Constants.Fields.FieldPrefix}{dateTimeOffsetRangeFilter.FieldName}_{Constants.Fields.DateTimeOffsets}";
                    searchQuery.AddRangeFilter(dateTimeOffsetRangeFieldName, dateTimeOffsetRangeFilter.ToNonNullableRangeFilter());
                    break;
                case DateTimeOffsetExactFilter dateTimeOffsetExactFilter:
                    var dateTimeOffsetExactFieldName = $"{Constants.Fields.FieldPrefix}{dateTimeOffsetExactFilter.FieldName}_{Constants.Fields.DateTimeOffsets}";
                    searchQuery.AddExactFilter(dateTimeOffsetExactFieldName, dateTimeOffsetExactFilter.ToDateTimeExactFilter());
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
                    searchQuery.WithFacets(facets => facets.FacetLongRange($"{Constants.Fields.FieldPrefix}{dateTimeOffsetRangeFacet.FieldName}_{Constants.Fields.DateTimeOffsets}", dateTimeOffsetRangeFacet.Ranges.Select(x => new Int64Range(x.Key, x.Min?.Ticks ?? DateTime.MinValue.Ticks, true, x.Max?.Ticks ?? DateTime.MaxValue.Ticks, true)).ToArray()));
                    break;
                case DecimalExactFacet decimalExactFacet:
                    searchQuery.WithFacets(facets => facets.FacetString($"{Constants.Fields.FieldPrefix}{decimalExactFacet.FieldName}_{Constants.Fields.Decimals}"));
                    break;
                case IntegerRangeFacet integerRangeFacet:
                    searchQuery.WithFacets(facets => facets.FacetLongRange($"{Constants.Fields.FieldPrefix}{integerRangeFacet.FieldName}_{Constants.Fields.Integers}", integerRangeFacet.Ranges.Select(x => new Int64Range(x.Key, x.Min ?? 0, true, x.Max ?? int.MaxValue, true)).ToArray()));
                    break;
                case KeywordFacet keywordFacet:
                    searchQuery.WithFacets(facets => facets.FacetString($"{Constants.Fields.FieldPrefix}{keywordFacet.FieldName}_{Constants.Fields.Texts}"));
                    break;
                case IntegerExactFacet integerExactFacet:
                    searchQuery.WithFacets(facets => facets.FacetString($"{Constants.Fields.FieldPrefix}{integerExactFacet.FieldName}_{Constants.Fields.Integers}"));
                    break;
                case DecimalRangeFacet decimalRangeFacet:
                {
                    var doubleRanges = decimalRangeFacet.Ranges.Select(x => new DoubleRange(x.Key, decimal.ToDouble(x.Min ?? 0) , true, decimal.ToDouble(x.Max ?? 0), true)).ToArray();
                    searchQuery.WithFacets(facets => facets.FacetDoubleRange($"{Constants.Fields.FieldPrefix}{facet.FieldName}_{Constants.Fields.Decimals}", doubleRanges));
                    break;
                }
                case DateTimeOffsetExactFacet dateTimeOffsetExactFacet:
                    searchQuery.WithFacets(facets => facets.FacetString($"{Constants.Fields.FieldPrefix}{dateTimeOffsetExactFacet.FieldName}_{Constants.Fields.DateTimeOffsets}"));
                    break;
            }
        }
    }
    
    private Document? MapToDocument(ISearchResult item)
    {
        if (Guid.TryParse(item.Id, out var guidId))
        {
            return new Document(guidId, UmbracoObjectTypes.Document);
        }

        // The id of an item may be appended with _{culture_{segment}, so strip those and map to guid.
        var indexofUnderscore = item.Id.IndexOf('_');
        var idWithOutCulture = item.Id.Remove(indexofUnderscore);
        if (Guid.TryParse(idWithOutCulture, out var idWithoutCultureGuid))
        {
            return new Document(idWithoutCultureGuid, UmbracoObjectTypes.Document);
        }

        return null;
    }
}