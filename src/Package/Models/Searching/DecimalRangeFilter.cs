namespace Package.Models.Searching;

public record DecimalRangeFilter(string Key, decimal? MinimumValue, decimal? MaximumValue, bool Negate)
    : RangeFilter<decimal?>(Key, MinimumValue, MaximumValue, Negate)
{
}