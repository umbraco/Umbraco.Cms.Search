using System.Collections;
using System.Globalization;
using Examine;
using Examine.Search;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Searching;
using Umbraco.Cms.Search.Core.Models.Searching.Faceting;
using Umbraco.Cms.Search.Core.Models.Searching.Filtering;
using Umbraco.Cms.Search.Core.Models.Searching.Sorting;
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
                        // Don't include oneself, this is for searches such as path id for ancestors.
                        searchQuery.Not().Field("Umb_Id_keywords", keywordFilterValue);
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
                    if (integerRangeFilter.Negate)
                    {
                        foreach (var integerRange in integerRangeFilter.Ranges)
                        {
                            searchQuery.Not().RangeQuery<int>([$"Umb_{integerRangeFilter.FieldName}_integers"], integerRange.MinimumValue, integerRange.MaximumValue);
                        }
                    }
                    else
                    {
                        var integerRanges = integerRangeFilter.Ranges;

                        if (integerRanges.Length == 1)
                        {
                            var integerRange = integerRanges[0];
                            searchQuery.And().RangeQuery<int>([$"Umb_{integerRangeFilter.FieldName}_integers"], integerRange.MinimumValue, integerRange.MaximumValue);
                        }
                        else
                        {
                            searchQuery.And().Group(query =>
                            {
                                var rangeQuery = query.RangeQuery<int>([$"Umb_{integerRangeFilter.FieldName}_integers"], integerRanges[0].MinimumValue, integerRanges[0].MaximumValue);
                                for (int i = 1; i < integerRanges.Length; i++)
                                {
                                    rangeQuery.Or().RangeQuery<int>([$"Umb_{integerRangeFilter.FieldName}_integers"], integerRanges[i].MinimumValue, integerRanges[i].MaximumValue);
                                }

                                return rangeQuery;
                            });
                        }
  
                    }
                    break;
                case IntegerExactFilter integerExactFilter:
                    if (integerExactFilter.Negate)
                    {
                        foreach (var integerFilterValue in integerExactFilter.Values)
                        {
                            searchQuery.Not().Group(query => query.Field($"Umb_{integerExactFilter.FieldName}_integers", integerFilterValue));
                        }
                    }
                    else
                    {
                        foreach (var integerFilterValue in integerExactFilter.Values)
                        {
                            searchQuery.And().Group(query => query.Field($"Umb_{integerExactFilter.FieldName}_integers", integerFilterValue));
                        }
                    }
                    break;
                case DecimalRangeFilter decimalRangeFilter:
                    if (decimalRangeFilter.Negate)
                    {
                        foreach (var decimalRange in decimalRangeFilter.Ranges)
                        {
                            searchQuery.Not().RangeQuery<double>([$"Umb_{decimalRangeFilter.FieldName}_decimals"], (double?)decimalRange.MinimumValue, (double?)decimalRange.MaximumValue);
                        }
                    }
                    else
                    {
                        // Examine does not support decimals out of the box, so we convert to double, we might loose some precision here (after 17 digits).
                        var decimalRanges = decimalRangeFilter.Ranges;

                        if (decimalRanges.Length == 1)
                        {
                            var integerRange = decimalRanges[0];
                            searchQuery.And().RangeQuery<double>([$"Umb_{decimalRangeFilter.FieldName}_decimals"], (double?)integerRange.MinimumValue, (double?)integerRange.MaximumValue);
                        }
                        else
                        {
                            searchQuery.And().Group(query =>
                            {
                                var rangeQuery = query.RangeQuery<double>([$"Umb_{decimalRangeFilter.FieldName}_decimals"], (double?)decimalRanges[0].MinimumValue, (double?)decimalRanges[0].MaximumValue);
                                for (int i = 1; i < decimalRanges.Length; i++)
                                {
                                    rangeQuery.Or().RangeQuery<double>([$"Umb_{decimalRangeFilter.FieldName}_decimals"], (double?)decimalRanges[i].MinimumValue, (double?)decimalRanges[i].MaximumValue);
                                }

                                return rangeQuery;
                            });
                        }
                    }
                    break;
                case DecimalExactFilter decimalExactFilter:
                    if (decimalExactFilter.Negate)
                    {
                        foreach (var decimalFilterValue in decimalExactFilter.Values)
                        {
                            // Examine does not support decimals out of the box, so convert to double, we might loose some precision here (after 17 digits).
                            searchQuery.Not().Group(query => query.Field($"Umb_{decimalExactFilter.FieldName}_decimals", (double)decimalFilterValue));
                        }
                    }
                    else
                    {
                        foreach (var decimalFilterValue in decimalExactFilter.Values)
                        {
                            // Examine does not support decimals out of the box, so convert to double, we might loose some precision here (after 17 digits).
                            searchQuery.And().Group(query => query.Field($"Umb_{decimalExactFilter.FieldName}_decimals", (double)decimalFilterValue));
                        }
                    }
                    break;
                case DateTimeOffsetRangeFilter dateTimeOffsetRangeFilter:
                    if (dateTimeOffsetRangeFilter.Negate)
                    {
                        foreach (var decimalRange in dateTimeOffsetRangeFilter.Ranges)
                        {
                            searchQuery.Not().RangeQuery<DateTime>([$"Umb_{dateTimeOffsetRangeFilter.FieldName}_datetimeoffsets"], decimalRange.MinimumValue?.DateTime, decimalRange.MaximumValue?.DateTime);
                        }
                    }
                    else
                    {
                        // Examine does not support decimals out of the box, so we convert to double, we might loose some precision here (after 17 digits).
                        var decimalRanges = dateTimeOffsetRangeFilter.Ranges;

                        if (decimalRanges.Length == 1)
                        {
                            var integerRange = decimalRanges[0];
                            searchQuery.And().RangeQuery<DateTime>([$"Umb_{dateTimeOffsetRangeFilter.FieldName}_datetimeoffsets"], integerRange.MinimumValue?.DateTime, integerRange.MaximumValue?.DateTime);
                        }
                        else
                        {
                            searchQuery.And().Group(query =>
                            {
                                var rangeQuery = query.RangeQuery<DateTime>([$"Umb_{dateTimeOffsetRangeFilter.FieldName}_datetimeoffsets"], decimalRanges[0].MinimumValue?.DateTime, decimalRanges[0].MaximumValue?.DateTime);
                                for (int i = 1; i < decimalRanges.Length; i++)
                                {
                                    rangeQuery.Or().RangeQuery<DateTime>([$"Umb_{dateTimeOffsetRangeFilter.FieldName}_datetimeoffsets"], decimalRanges[i].MinimumValue?.DateTime, decimalRanges[i].MaximumValue?.DateTime);
                                }

                                return rangeQuery;
                            });
                        }
                    }
                    break;
                case DateTimeOffsetExactFilter dateTimeOffsetExactFilter:
                    if (dateTimeOffsetExactFilter.Negate)
                    {
                        foreach (var dateTimeOffsetFilterValue in dateTimeOffsetExactFilter.Values)
                        {
                            searchQuery.Not().Group(query => query.Field($"Umb_{dateTimeOffsetExactFilter.FieldName}_datetimeoffsets", dateTimeOffsetFilterValue.DateTime));
                        }
                    }
                    else
                    {
                        foreach (var dateTimeOffsetFilterValue in dateTimeOffsetExactFilter.Values)
                        {
                            searchQuery.And().Group(query => query.Field($"Umb_{dateTimeOffsetExactFilter.FieldName}_datetimeoffsets", dateTimeOffsetFilterValue.DateTime));
                        }
                    }
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