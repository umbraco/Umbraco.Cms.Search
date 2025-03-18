using Umbraco.Cms.Search.Core.Models.Searching.Faceting;

namespace Umbraco.Cms.Search.Core.Models.Searching;

// TODO: should probably yield a list of [key, contentTypeAlias] or [key, objectType] to better facilitate indexes with multiple types
public record SearchResult(long Total, IEnumerable<Guid> Keys, IEnumerable<FacetResult> Facets)
{
}