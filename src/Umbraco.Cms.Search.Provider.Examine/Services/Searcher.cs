using System.Globalization;
using System.Text;
using Examine;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Searching;
using Umbraco.Cms.Search.Core.Models.Searching.Faceting;
using Umbraco.Cms.Search.Core.Models.Searching.Filtering;
using Umbraco.Cms.Search.Core.Models.Searching.Sorting;
using Umbraco.Extensions;
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

        if (query is null)
        {
            if (culture is not null || segment is not null)
            {
                var noQueryQuery = index.Searcher.CreateQuery().NativeQuery($"+(+culture:\"{culture ?? "none"}\")").And().NativeQuery($"+(+segment:\"{segment ?? "none"}\")");
                var cultureAndSegmentQuery = noQueryQuery.Execute();
                return await Task.FromResult(new SearchResult(cultureAndSegmentQuery.TotalItemCount, cultureAndSegmentQuery.Select(MapToDocument).WhereNotNull(), Array.Empty<FacetResult>()));
            }

            var all = index.Searcher.CreateQuery().All().Execute();
            return await Task.FromResult(new SearchResult(all.TotalItemCount, all.Select(MapToDocument).WhereNotNull(), Array.Empty<FacetResult>()));
        }

        // We have to do to lower on all queries.
        var searchQuery = index.Searcher.CreateQuery().ManagedQuery(culture is null ? query.ToLowerInvariant() : query.ToLower(new CultureInfo(culture)));
        
        searchQuery.And().NativeQuery($"+(+culture:\"{culture ?? "none"}\")");
        searchQuery.And().NativeQuery($"+(+segment:\"{segment ?? "none"}\")");

        var results = searchQuery.Execute();
        
        
        return await Task.FromResult(new SearchResult(results.TotalItemCount, results.Select(MapToDocument).WhereNotNull(), Array.Empty<FacetResult>()));
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