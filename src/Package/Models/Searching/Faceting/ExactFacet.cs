namespace Package.Models.Searching.Faceting;

public abstract record ExactFacet(string FieldName) : Facet(FieldName)
{
}