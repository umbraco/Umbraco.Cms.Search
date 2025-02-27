namespace Package.Models.Searching;

public record IntegerExactFilter(string Key, int[] Values, bool Negate) : ExactFilter<int>(Key, Values, Negate)
{
}