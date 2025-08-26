namespace Umbraco.Cms.Search.Provider.Examine.Configuration;

public class FieldOptions
{
    public required Field[] Fields { get; set; } = [];

    public class Field
    {
        public required string PropertyName { get; init; }

        public required FieldValues FieldValues { get; init; }

        public bool Sortable { get; init; }

        public bool Facetable { get; init; }
    }
}
