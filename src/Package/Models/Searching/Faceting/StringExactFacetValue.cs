namespace Package.Models.Searching.Faceting;

public record StringExactFacetValue(string Key, long Count) : ExactFacetValue<string>(Key, Count)
{
}