using Package.Models.Searching;

namespace Package.Services;

public interface ISearchService
{
    // TODO: support sorting
    // TODO: support protected content
    Task<SearchResult> SearchAsync(string? query, IEnumerable<Filter>? filters, IEnumerable<Facet>? facets, string? culture, string? segment, int skip, int take);
}
