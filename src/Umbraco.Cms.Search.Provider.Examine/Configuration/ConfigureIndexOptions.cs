using Examine;
using Examine.Lucene;
using Microsoft.Extensions.Options;

namespace Umbraco.Cms.Search.Provider.Examine.Configuration;

public sealed class ConfigureIndexOptions : IConfigureNamedOptions<LuceneDirectoryIndexOptions>
{
    private readonly IOptions<FacetOptions> _facetOptions;

    public ConfigureIndexOptions(IOptions<FacetOptions> facetOptions)
        => _facetOptions = facetOptions;

    public void Configure(string? name, LuceneDirectoryIndexOptions options)
        => AddOptions(options);

    public void Configure(LuceneDirectoryIndexOptions options)
        => Configure(string.Empty, options);

    private void AddOptions(LuceneDirectoryIndexOptions options)
    {
        foreach (FacetOptions.Field facetEntry in _facetOptions.Value.Fields)
        {
            var fieldPostfix = facetEntry.FieldValues switch
            {
                FieldValues.Texts => Constants.Fields.Texts,
                FieldValues.TextsR1 => Constants.Fields.TextsR1,
                FieldValues.TextsR2 => Constants.Fields.TextsR2,
                FieldValues.TextsR3 => Constants.Fields.TextsR3,
                FieldValues.Integers => Constants.Fields.Integers,
                FieldValues.Decimals => Constants.Fields.Decimals,
                FieldValues.DateTimeOffsets => Constants.Fields.DateTimeOffsets,
                FieldValues.Keywords => Constants.Fields.Keywords,
                _ => throw new ArgumentOutOfRangeException(nameof(facetEntry.FieldValues))
            };

            var fieldDefinitionType = facetEntry.FieldValues switch
            {
                FieldValues.Texts or FieldValues.TextsR1 or FieldValues.TextsR2 or FieldValues.TextsR3 or FieldValues.Keywords
                    => facetEntry is { Sortable: true, Facetable: true }
                        ? FieldDefinitionTypes.FacetFullTextSortable
                        : facetEntry.Facetable
                            ? FieldDefinitionTypes.FacetFullText
                            : facetEntry.Sortable
                                ? FieldDefinitionTypes.FullTextSortable
                                : FieldDefinitionTypes.FullText,
                FieldValues.Integers => facetEntry.Facetable
                    ? FieldDefinitionTypes.FacetInteger
                    : FieldDefinitionTypes.Integer,
                FieldValues.Decimals => facetEntry.Facetable
                    ? FieldDefinitionTypes.FacetDouble
                    : FieldDefinitionTypes.Double,
                FieldValues.DateTimeOffsets => facetEntry.Facetable
                    ? FieldDefinitionTypes.FacetDateTime
                    : FieldDefinitionTypes.DateTime,
                _ => throw new ArgumentOutOfRangeException(nameof(facetEntry.FieldValues))
            };

            var fieldName = $"{Constants.Fields.FieldPrefix}{facetEntry.PropertyName}_{fieldPostfix}";
            // options.FacetsConfig.SetMultiValued(fieldName, true);
            options.FieldDefinitions.AddOrUpdate(new FieldDefinition(fieldName, fieldDefinitionType));
        }
    }
}
