namespace Package.Models.Searching;

public record StringExactFilter(string Key, string[] Values, bool Negate) : ExactFilter<string>(Key, Values, Negate)
{
}