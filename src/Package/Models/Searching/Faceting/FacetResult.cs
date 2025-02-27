namespace Package.Models.Searching.Faceting;

public record FacetResult(string FieldName, IEnumerable<FacetValue> Values)
{
}