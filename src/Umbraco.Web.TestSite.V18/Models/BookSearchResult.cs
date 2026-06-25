using Umbraco.Cms.Core.Models.DeliveryApi;
using Umbraco.Cms.Search.Core.Models.Searching.Faceting;

namespace Site.Models;

public class BookSearchResult
{
    public required long Total { get; init; }
    
    public required FacetResult[] Facets { get; init; }

    public required IApiContent[] Documents { get; init; }
}