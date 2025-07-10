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
using FacetValue = Umbraco.Cms.Search.Core.Models.Searching.Faceting.FacetValue;
using ISearcher = Umbraco.Cms.Search.Core.Services.ISearcher;
using SearchResult = Umbraco.Cms.Search.Core.Models.Searching.SearchResult;
using IndexValue = Umbraco.Cms.Search.Core.Models.Indexing.IndexValue;

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

        if (query is null)
        {
            if (culture is not null || segment is not null)
            {
                var noQueryQuery = index.Searcher.CreateQuery().NativeQuery($"+(+culture:\"{culture ?? "none"}\")").And().NativeQuery($"+(+segment:\"{segment ?? "none"}\")");
                var cultureAndSegmentQuery = noQueryQuery.Execute();
                return await Task.FromResult(new SearchResult(cultureAndSegmentQuery.TotalItemCount, cultureAndSegmentQuery.Select(MapToDocument).WhereNotNull(), Array.Empty<FacetResult>()));
            }

            var allQuery = index.Searcher.CreateQuery().All();
            AddFacets(allQuery, facets);
            var all = allQuery.Execute();
            all.GetFacets();
            return await Task.FromResult(new SearchResult(all.TotalItemCount, all.Select(MapToDocument).WhereNotNull(), facets is null ? Array.Empty<FacetResult>() : MapFacets(all, facets)));
        }

        // We have to do to lower on all queries.
        var searchQuery = index.Searcher.CreateQuery().ManagedQuery(culture is null ? query.ToLowerInvariant() : query.ToLower(new CultureInfo(culture)));
        
        searchQuery.And().NativeQuery($"+(+Umb_culture:\"{culture ?? "none"}\")");
        searchQuery.And().NativeQuery($"+(+Umb_segment:\"{segment ?? "none"}\")");
        
        // Add facets
        AddFacets(searchQuery, facets);
        var results = searchQuery.Execute();
        
        
        return await Task.FromResult(new SearchResult(results.TotalItemCount, results.Select(MapToDocument).WhereNotNull(), Array.Empty<FacetResult>()));
    }

    private IEnumerable<FacetResult> MapFacets(ISearchResults results, IEnumerable<Facet> queryFacets)
    {
        var facetResults = new List<FacetResult>();
        foreach (var facet in queryFacets)
        {
            var fieldName = $"Umb_{facet.FieldName}_integers";
            var facetForFacet = results.GetFacet(fieldName);
            if (facetForFacet is null)
            {
                continue;
            }
            if (facet is IntegerRangeFacet integerRangeFacet)
            {
                var result = integerRangeFacet.Ranges.Select(x =>
                {
                    int value = (int?) facetForFacet.Facet(x.Key)?.Value ?? 0;
                    return new IntegerRangeFacetValue(x.Key, x.Min, x.Max, value);
                });
                facetResults.Add(new FacetResult(facet.FieldName, result));
            }
        }

        return facetResults;
    }

    private void AddFacets(IOrdering searchQuery, IEnumerable<Facet>? facets)
    {
        if (facets is null)
        {
            return;
        }

        foreach (var facet in facets)
        {
            if (facet is IntegerRangeFacet integerRangeFacet)
            {
                searchQuery.WithFacets(facets => facets.FacetLongRange($"Umb_{integerRangeFacet.FieldName}_integers", integerRangeFacet.Ranges.Select(x => new Int64Range(x.Key, x.Min ?? 0, true, x.Max ?? int.MaxValue, true)).ToArray()));
            }
        }
    }
    
    IEnumerable<FacetValue> GetFacetValues(Facet facet, IEnumerable<IndexValue> values)
        => facet switch
        {
            // KeywordFacet => values.SelectMany(v => v.Keywords ?? []).GroupBy(v => v).Select(g => new KeywordFacetValue(g.Key, g.Count())),
            // IntegerExactFacet => values.SelectMany(v => v.Integers ?? []).GroupBy(v => v).Select(g => new IntegerExactFacetValue(g.Key, g.Count())),
            // DecimalExactFacet => values.SelectMany(v => v.Decimals ?? []).GroupBy(v => v).Select(g => new DecimalExactFacetValue(g.Key, g.Count())),
            // DateTimeOffsetExactFacet => values.SelectMany(v => v.DateTimeOffsets ?? []).GroupBy(v => v).Select(g => new DateTimeOffsetExactFacetValue(g.Key, g.Count())),
            IntegerRangeFacet integerRangeFacet => ExtractIntegerRangeFacetValues(integerRangeFacet, values), 
            // DecimalRangeFacet decimalRangeFacet => ExtractDecimalRangeFacetValues(decimalRangeFacet, values),
            // DateTimeOffsetRangeFacet dateTimeOffsetRangeFacet => ExtractDateTimeOffsetRangeFacetValues(dateTimeOffsetRangeFacet, values),
            _ => throw new ArgumentOutOfRangeException(nameof(facet), $"Encountered an unsupported facet type: {facet.GetType().Name}")
        }; 
    
    
    private IntegerRangeFacetValue[] ExtractIntegerRangeFacetValues(IntegerRangeFacet facet, IEnumerable<IndexValue> values)
    {
        var allValues = values.SelectMany(v => v.Integers ?? []).ToArray();
        return facet
            .Ranges
            .Select(range => new IntegerRangeFacetValue(
                range.Key,
                range.Min,
                range.Max,
                allValues.Count(v => v > (range.Min ?? int.MinValue) && v <= (range.Max ?? int.MaxValue)))
            ).ToArray();
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
    
    
    private string ToUTF8(string text)
    {
        return Encoding.UTF8.GetString(Encoding.Default.GetBytes(text));
    }
}