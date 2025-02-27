namespace Package.Models.Searching;

public record StringExactFacetValue(string Key, long Count) : ExactFacetValue<string>(Key, Count)
{
}