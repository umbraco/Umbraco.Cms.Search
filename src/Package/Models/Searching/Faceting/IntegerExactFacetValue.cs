namespace Package.Models.Searching.Faceting;

public record IntegerExactFacetValue(int Key, long Count) : ExactFacetValue<int>(Key, Count)
{
}