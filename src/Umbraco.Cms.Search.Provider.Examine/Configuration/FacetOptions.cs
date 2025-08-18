namespace Umbraco.Cms.Search.Provider.Examine.Configuration;

// TODO KJA: rename to FieldOptions
public class FacetOptions
{
    public Field[] Fields { get; set; } = [];

    public class Field
    {
        public required string PropertyName { get; set; } = string.Empty;

        public required FieldValues FieldValues { get; init; }

        public bool Sortable { get; init; }

        public bool Facetable { get; init; }
    }
}

// TODO KJA: move this elsewhere
public enum FieldValues
{
    Texts,
    TextsR1,
    TextsR2,
    TextsR3,
    Integers,
    Decimals,
    DateTimeOffsets,
    Keywords,
}
