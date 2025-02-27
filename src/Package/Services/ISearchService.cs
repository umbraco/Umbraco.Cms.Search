using Package.Models.Searching;
using Package.Models.Searching.Faceting;
using Package.Models.Searching.Filtering;
using Package.Models.Searching.Sorting;

namespace Package.Services;

public interface ISearchService
{
    // TODO: support protected content
    Task<SearchResult> SearchAsync(string? query, IEnumerable<Filter>? filters, IEnumerable<Facet>? facets, IEnumerable<Sorter>? sorters, string? culture, string? segment, int skip, int take);
}
