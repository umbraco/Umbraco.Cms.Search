namespace Package.Models.Searching;

public record DateTimeOffsetRangeFilter(string Key, DateTimeOffset? MinimumValue, DateTimeOffset? MaximumValue, bool Negate)
    : RangeFilter<DateTimeOffset?>(Key, MinimumValue, MaximumValue, Negate)
{
}