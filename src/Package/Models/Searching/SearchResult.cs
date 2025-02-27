namespace Package.Models.Searching;

public record SearchResult(long Total, IEnumerable<Guid> Ids, IEnumerable<FacetResult> Facets)
{
}