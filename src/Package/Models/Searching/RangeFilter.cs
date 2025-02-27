namespace Package.Models.Searching;

public abstract record RangeFilter<T>(string Key, T MinimumValue, T MaximumValue, bool Negate) : Filter(Key, Negate)
{
}