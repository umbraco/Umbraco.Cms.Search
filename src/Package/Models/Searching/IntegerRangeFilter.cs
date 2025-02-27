namespace Package.Models.Searching;

public record IntegerRangeFilter(string Key, int? MinimumValue, int? MaximumValue, bool Negate)
    : RangeFilter<int?>(Key, MinimumValue, MaximumValue, Negate)
{
}