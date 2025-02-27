namespace Package.Models.Searching;

public abstract record ContainsFilter<T>(string Key, T[] Values, bool Negate) : Filter(Key, Negate)
{
}