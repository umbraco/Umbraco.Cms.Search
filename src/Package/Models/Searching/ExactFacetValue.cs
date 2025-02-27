namespace Package.Models.Searching;

public abstract record ExactFacetValue<T>(T Key, long Count) : FacetValue(Count)
{
}