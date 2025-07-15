using System.Collections;
using System.Globalization;
using Examine;
using Examine.Search;
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
        if (_examineManager.TryGetIndex(indexAlias, out var index) is false)
        {
            return await Task.FromResult(new SearchResult(0, Array.Empty<Document>(), Array.Empty<FacetResult>()));
        }

        var searchQuery = index.Searcher.CreateQuery().NativeQuery($"+(+Umb_culture:\"{culture ?? "none"}\")").And().NativeQuery($"+(+Umb_segment:\"{segment ?? "none"}\")");
        if (query is not null)
        {
            // We have to do to lower on all queries.
            searchQuery.And().ManagedQuery(culture is null ? query.ToLowerInvariant() : query.ToLower(new CultureInfo(culture)));
        }
        
        // Add facets and filters
        AddFilters(searchQuery, filters);
        AddFacets(searchQuery, facets);
        var results = searchQuery.Execute();
        
        return await Task.FromResult(new SearchResult(results.TotalItemCount, results.Select(MapToDocument).WhereNotNull(), facets is null ? Array.Empty<FacetResult>() : _examineMapper.MapFacets(results, facets)));
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
                    if (keywordFilter.Negate)
                    {
                        searchQuery.Not().Field($"{keywordFilter.FieldName}_{Constants.Fields.Keywords}", keywordFilterValue);
                    }
                    else
                    {
                        var field = keywordFilter.FieldName.StartsWith(Constants.Fields.FieldPrefix) ? $"{keywordFilter.FieldName}_{Constants.Fields.Keywords}" : $"{Constants.Fields.FieldPrefix}{keywordFilter.FieldName}_{Constants.Fields.Keywords}";
                        searchQuery.And().Field(field, string.Join(" ", keywordFilterValue));
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
                        searchQuery.And().Field($"{Constants.Fields.FieldPrefix}{textFilter.FieldName}_{Constants.Fields.Texts}", textFilterValue);
                        searchQuery.And().Field($"{Constants.Fields.FieldPrefix}{textFilter.FieldName}_{Constants.Fields.TextsR1}", textFilterValue.Boost(30));
                        searchQuery.And().Field($"{Constants.Fields.FieldPrefix}{textFilter.FieldName}_{Constants.Fields.TextsR2}", textFilterValue.Boost(20));
                        searchQuery.And().Field($"{Constants.Fields.FieldPrefix}{textFilter.FieldName}_{Constants.Fields.TextsR3}", textFilterValue.Boost(10));
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
        // We have to get the Id out, as the key is culture specific
        if(Guid.TryParse(item.Values.Where(x => x.Key == $"{Constants.Fields.FieldPrefix}Id_{Constants.Fields.Keywords}").Select(x => x.Value).First(), out var id) is false)
        {
            return null;
        }
       
        return new Document(id, UmbracoObjectTypes.Document);
    }
}