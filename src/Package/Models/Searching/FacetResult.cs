namespace Package.Models.Searching;

public record FacetResult(string Key, IEnumerable<FacetValue> Values)
{
}