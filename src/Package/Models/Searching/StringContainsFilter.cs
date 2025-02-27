namespace Package.Models.Searching;

public record StringContainsFilter(string Key, string[] Values, bool Negate) : ContainsFilter<string>(Key, Values, Negate)
{
}