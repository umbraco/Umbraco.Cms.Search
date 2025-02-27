namespace Package.Models.Searching;

public abstract record Filter(string Key, bool Negate)
{
}