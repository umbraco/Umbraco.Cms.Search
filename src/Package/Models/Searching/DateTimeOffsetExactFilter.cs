namespace Package.Models.Searching;

public record DateTimeOffsetExactFilter(string Key, DateTimeOffset[] Values, bool Negate) : ExactFilter<DateTimeOffset>(Key, Values, Negate)
{
}