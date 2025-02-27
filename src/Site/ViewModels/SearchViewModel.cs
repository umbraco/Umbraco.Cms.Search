using Package.Models.Searching.Faceting;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace Site.ViewModels;

public class SearchViewModel
{
    public long Total { get; init; }

    public required FacetResult[] Facets { get; init; }

    public required IPublishedContent[] Documents { get; init; }
}