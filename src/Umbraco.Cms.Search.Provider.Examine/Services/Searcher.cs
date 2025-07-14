using System.Globalization;
using System.Text;
using Examine;
using Examine.Lucene;
using Examine.Search;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Searching;
using Umbraco.Cms.Search.Core.Models.Searching.Faceting;
using Umbraco.Cms.Search.Core.Models.Searching.Filtering;
using Umbraco.Cms.Search.Core.Models.Searching.Sorting;
using Umbraco.Extensions;
using FacetResult = Umbraco.Cms.Search.Core.Models.Searching.Faceting.FacetResult;
using ISearcher = Umbraco.Cms.Search.Core.Services.ISearcher;
using SearchResult = Umbraco.Cms.Search.Core.Models.Searching.SearchResult;

namespace Umbraco.Cms.Search.Provider.Examine.Services;

public class Searcher : ISearcher
{
    private readonly IExamineManager _examineManager;

    public Searcher(IExamineManager examineManager)
    {
        _examineManager = examineManager;
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
        
        // Add facets
        AddFacets(searchQuery, facets);
        var results = searchQuery.Execute();
        
        return await Task.FromResult(new SearchResult(results.TotalItemCount, results.Select(MapToDocument).WhereNotNull(), facets is null ? Array.Empty<FacetResult>() : MapFacets(results, facets)));
    }

    private IEnumerable<FacetResult> MapFacets(ISearchResults searchResults, IEnumerable<Facet> queryFacets)
    {
        var facetResults = new List<FacetResult>();
        foreach (var facet in queryFacets)
        {
            switch (facet)
            {
                case IntegerRangeFacet integerRangeFacet:
                {
                    var integerRangeFacetResult = integerRangeFacet.Ranges.Select(x =>
                    {
                        
                        int value = GetFacetCount($"Umb_{facet.FieldName}_integers", x.Key, searchResults);
                        return new IntegerRangeFacetValue(x.Key, x.Min, x.Max, value);
                    });
                    facetResults.Add(new FacetResult(facet.FieldName, integerRangeFacetResult));
                    break;
                }
                case IntegerExactFacet integerExactFacet:
                    var examineIntegerFacets = searchResults.GetFacet($"Umb_{integerExactFacet.FieldName}_integers");
                    if (examineIntegerFacets is null)
                    {
                        continue;
                    }
                    
                    foreach (var integerExactFacetValue in examineIntegerFacets)
                    {
                        if (int.TryParse(integerExactFacetValue.Label, out var labelValue) is false)
                        {
                            // Cannot convert the label to int, skipping.
                            continue;
                        }
                        facetResults.Add(new FacetResult(facet.FieldName, [new IntegerExactFacetValue(labelValue, (int?)integerExactFacetValue.Value ?? 0)
                        ]));
                    }
                    break;
                case DecimalRangeFacet decimalRangeFacet:
                    var decimalRangeFacetResult = decimalRangeFacet.Ranges.Select(x =>
                    {
                        int value = GetFacetCount($"Umb_{facet.FieldName}_decimals", x.Key, searchResults);
                        return new DecimalRangeFacetValue(x.Key, x.Min, x.Max, value);
                    });
                    facetResults.Add(new FacetResult(facet.FieldName, decimalRangeFacetResult));
                    break;
                case DecimalExactFacet decimalExactFacet:
                    var examineDecimalFacets = searchResults.GetFacet($"Umb_{decimalExactFacet.FieldName}_decimals");
                    if (examineDecimalFacets is null)
                    {
                        continue;
                    }
                    
                    foreach (var decimalExactFacetValue in examineDecimalFacets)
                    {
                        if (decimal.TryParse(decimalExactFacetValue.Label, out var labelValue) is false)
                        {
                            // Cannot convert the label to decimal, skipping.
                            continue;
                        }
                        facetResults.Add(new FacetResult(facet.FieldName, [new DecimalExactFacetValue(labelValue, (int?)decimalExactFacetValue.Value ?? 0)
                        ]));
                    }
                    break;
                case KeywordFacet keywordFacet:
                    var examineKeywordFacets = searchResults.GetFacet($"Umb_{keywordFacet.FieldName}_texts");
                    if (examineKeywordFacets is null)
                    {
                        continue;
                    }

                    foreach (var examineKeywordFacet in examineKeywordFacets)
                    {
                        facetResults.Add(new FacetResult(facet.FieldName, new []{ new KeywordFacetValue(examineKeywordFacet.Label, (int?)examineKeywordFacet.Value ?? 0)}));
                    }
                    break;
                 case DateTimeOffsetRangeFacet dateTimeOffsetRangeFacet:
                    var dateTimeOffsetRangeFacetResult = dateTimeOffsetRangeFacet.Ranges.Select(x =>
                    {
                        int value = GetFacetCount($"Umb_{facet.FieldName}_datetimeoffsets", x.Key, searchResults);
                        return new DateTimeOffsetRangeFacetValue(x.Key, x.Min, x.Max, value);
                    });
                    facetResults.Add(new FacetResult(facet.FieldName, dateTimeOffsetRangeFacetResult));
                    break;
            }
        }

        return facetResults;
    }

    private int GetFacetCount(string fieldName, string key, ISearchResults results)
    {
        return (int?)results.GetFacet(fieldName)?.Facet(key)?.Value ?? 0;
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
                    throw new NotImplementedException();
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