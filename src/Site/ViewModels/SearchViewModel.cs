using Package.Models.Searching.Faceting;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace Site.ViewModels;

public class SearchViewModel
{
    public long? Total { get; init; }

    public FacetResult[]? Facets { get; init; }

    public IPublishedContent[]? Documents { get; init; }
}