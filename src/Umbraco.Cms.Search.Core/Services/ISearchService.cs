using Umbraco.Cms.Search.Core.Models.Searching;
using Umbraco.Cms.Search.Core.Models.Searching.Faceting;
using Umbraco.Cms.Search.Core.Models.Searching.Filtering;
using Umbraco.Cms.Search.Core.Models.Searching.Sorting;

namespace Umbraco.Cms.Search.Core.Services;

public interface ISearchService
{
    // TODO: support protected content
    Task<SearchResult> SearchAsync(string? query, IEnumerable<Filter>? filters, IEnumerable<Facet>? facets, IEnumerable<Sorter>? sorters, string? culture, string? segment, int skip, int take);
}
