using System.Text.Json.Serialization;

namespace Package.Models.Searching;

// TODO: handle polymorphism in a JsonTypeInfoResolver modifier
[JsonDerivedType(typeof(StringExactFacetValue))]
[JsonDerivedType(typeof(IntegerExactFacetValue))]
public abstract record FacetValue(long Count)
{
}