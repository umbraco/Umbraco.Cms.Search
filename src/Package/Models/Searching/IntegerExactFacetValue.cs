namespace Package.Models.Searching;

public record IntegerExactFacetValue(int Key, long Count) : ExactFacetValue<int>(Key, Count)
{
}