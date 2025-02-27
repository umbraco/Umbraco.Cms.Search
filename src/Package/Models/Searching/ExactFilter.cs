namespace Package.Models.Searching;

public abstract record ExactFilter<T>(string Key, T[] Values, bool Negate) : Filter(Key, Negate)
{
}