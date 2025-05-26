using Umbraco.Cms.Search.Core.Models.Searching;
using Umbraco.Cms.Search.Core.Models.Searching.Faceting;
using Umbraco.Cms.Search.Core.Models.Searching.Filtering;
using Umbraco.Cms.Search.Core.Models.Searching.Sorting;

namespace Umbraco.Cms.Search.Core.Services;

public interface ISearcher
{
    Task<SearchResult> SearchAsync(
        string indexAlias,
        string? query,
        IEnumerable<Filter>? filters,
        IEnumerable<Facet>? facets,
        IEnumerable<Sorter>? sorters,
        string? culture,
        string? segment,
        AccessContext? accessContext,
        int skip,
        int take);
}
