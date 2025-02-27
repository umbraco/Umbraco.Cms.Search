namespace Package.Models.Searching.Faceting;

public abstract record ExactFacetValue<T>(T Key, long Count) : FacetValue(Count)
{
}