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
                        searchQuery.Not().Field($"{keywordFilter.FieldName}_keywords", keywordFilterValue);
                    }
                    else
                    {
                        var field = keywordFilter.FieldName.StartsWith("Umb_") ? $"{keywordFilter.FieldName}_keywords" : $"Umb_{keywordFilter.FieldName}_keywords";
                        searchQuery.And().Field(field, string.Join(" ", keywordFilterValue));
                    }
                    break;
                case TextFilter textFilter:
                    var textFilterValue = string.Join(" ", textFilter.Values);
                    if (textFilter.Negate)
                    {
                        searchQuery.Not().Field($"Umb_{textFilter.FieldName}_texts", textFilterValue);
                    }
                    else
                    {
                        searchQuery.And().Field($"Umb_{textFilter.FieldName}_texts", textFilterValue);
                    }
                    break;
                case IntegerRangeFilter integerRangeFilter:
                    var integerRangeFieldName = $"Umb_{integerRangeFilter.FieldName}_integers";
                    searchQuery.AddRangeFilter(integerRangeFieldName, integerRangeFilter.ToNonNullableRangeFilter());
                    break;
                case IntegerExactFilter integerExactFilter:
                    var integerExactFieldName = $"Umb_{integerExactFilter.FieldName}_integers";
                    searchQuery.AddExactFilter(integerExactFieldName, integerExactFilter);
                    break;
                case DecimalRangeFilter decimalRangeFilter:
                    var decimalRangeFieldName = $"Umb_{decimalRangeFilter.FieldName}_decimals";
                    searchQuery.AddRangeFilter(decimalRangeFieldName, decimalRangeFilter.ToNonNullableRangeFilter());
                    break;
                case DecimalExactFilter decimalExactFilter:
                    var decimalExactFieldName = $"Umb_{decimalExactFilter.FieldName}_decimals";
                    searchQuery.AddExactFilter(decimalExactFieldName, decimalExactFilter.ToDoubleExactFilter());
                    break;
                case DateTimeOffsetRangeFilter dateTimeOffsetRangeFilter:
                    var dateTimeOffsetRangeFieldName = $"Umb_{dateTimeOffsetRangeFilter.FieldName}_datetimeoffsets";
                    searchQuery.AddRangeFilter(dateTimeOffsetRangeFieldName, dateTimeOffsetRangeFilter.ToNonNullableRangeFilter());
                    break;
                case DateTimeOffsetExactFilter dateTimeOffsetExactFilter:
                    var dateTimeOffsetExactFieldName = $"Umb_{dateTimeOffsetExactFilter.FieldName}_datetimeoffsets";
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
                    searchQuery.WithFacets(facets => facets.FacetLongRange($"Umb_{dateTimeOffsetRangeFacet.FieldName}_datetimeoffsets", dateTimeOffsetRangeFacet.Ranges.Select(x => new Int64Range(x.Key, x.Min?.Ticks ?? DateTime.MinValue.Ticks, true, x.Max?.Ticks ?? DateTime.MaxValue.Ticks, true)).ToArray()));
                    break;
                case DecimalExactFacet decimalExactFacet:
                    searchQuery.WithFacets(facets => facets.FacetString($"Umb_{decimalExactFacet.FieldName}_decimals"));
                    break;
                case IntegerRangeFacet integerRangeFacet:
                    searchQuery.WithFacets(facets => facets.FacetLongRange($"Umb_{integerRangeFacet.FieldName}_integers", integerRangeFacet.Ranges.Select(x => new Int64Range(x.Key, x.Min ?? 0, true, x.Max ?? int.MaxValue, true)).ToArray()));
                    break;
                case KeywordFacet keywordFacet:
                    searchQuery.WithFacets(facets => facets.FacetString($"Umb_{keywordFacet.FieldName}_texts"));
                    break;
                case IntegerExactFacet integerExactFacet:
                    searchQuery.WithFacets(facets => facets.FacetString($"Umb_{integerExactFacet.FieldName}_integers"));
                    break;
                case DecimalRangeFacet decimalRangeFacet:
                {
                    var doubleRanges = decimalRangeFacet.Ranges.Select(x => new DoubleRange(x.Key, decimal.ToDouble(x.Min ?? 0) , true, decimal.ToDouble(x.Max ?? 0), true)).ToArray();
                    searchQuery.WithFacets(facets => facets.FacetDoubleRange($"Umb_{facet.FieldName}_decimals", doubleRanges));
                    break;
                }
                case DateTimeOffsetExactFacet dateTimeOffsetExactFacet:
                    searchQuery.WithFacets(facets => facets.FacetString($"Umb_{dateTimeOffsetExactFacet.FieldName}_datetimeoffsets"));
                    break;
            }
        }
    }
    
    private Document? MapToDocument(ISearchResult item)
    {
        // We have to get the Id out, as the key is culture specific
        if(Guid.TryParse(item.Values.Where(x => x.Key == "Umb_Id_keywords").Select(x => x.Value).First(), out var id) is false)
        {
            return null;
        }
       
        return new Document(id, UmbracoObjectTypes.Document);
    }
}