namespace Package.Models.Searching;

public abstract record ExactFacet(string Key) : Facet(Key)
{
}