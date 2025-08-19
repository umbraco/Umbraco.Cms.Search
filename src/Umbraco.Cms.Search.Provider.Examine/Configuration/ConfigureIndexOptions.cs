using Examine;
using Examine.Lucene;
using Microsoft.Extensions.Options;

namespace Umbraco.Cms.Search.Provider.Examine.Configuration;

public sealed class ConfigureIndexOptions : IConfigureNamedOptions<LuceneDirectoryIndexOptions>
{
    private readonly FieldOptions _fieldOptions;

    public ConfigureIndexOptions(IOptions<FieldOptions> options)
        => _fieldOptions = options.Value;

    public void Configure(string? name, LuceneDirectoryIndexOptions options)
        => AddOptions(options);

    public void Configure(LuceneDirectoryIndexOptions options)
        => Configure(string.Empty, options);

    private void AddSystemFields(LuceneDirectoryIndexOptions options)
    {
        options.FieldDefinitions.AddOrUpdate(new FieldDefinition($"{Constants.Fields.SystemFieldPrefix}{Constants.Fields.SystemFields.Id}", FieldDefinitionTypes.FullText));
        options.FieldDefinitions.AddOrUpdate(new FieldDefinition($"{Constants.Fields.SystemFieldPrefix}{Constants.Fields.SystemFields.ParentId}", FieldDefinitionTypes.FullText));
        options.FieldDefinitions.AddOrUpdate(new FieldDefinition($"{Constants.Fields.SystemFieldPrefix}{Constants.Fields.SystemFields.PathIds}", FieldDefinitionTypes.FullText));
        options.FieldDefinitions.AddOrUpdate(new FieldDefinition($"{Constants.Fields.SystemFieldPrefix}{Constants.Fields.SystemFields.ContentTypeId}", FieldDefinitionTypes.FullText));
        options.FieldDefinitions.AddOrUpdate(new FieldDefinition($"{Constants.Fields.SystemFieldPrefix}{Constants.Fields.SystemFields.CreateDate}", FieldDefinitionTypes.DateTime));
        options.FieldDefinitions.AddOrUpdate(new FieldDefinition($"{Constants.Fields.SystemFieldPrefix}{Constants.Fields.SystemFields.UpdateDate}", FieldDefinitionTypes.DateTime));
        options.FieldDefinitions.AddOrUpdate(new FieldDefinition($"{Constants.Fields.SystemFieldPrefix}{Constants.Fields.SystemFields.Level}", FieldDefinitionTypes.Integer));
        options.FieldDefinitions.AddOrUpdate(new FieldDefinition($"{Constants.Fields.SystemFieldPrefix}{Constants.Fields.SystemFields.ObjectType}", FieldDefinitionTypes.Integer));
        options.FieldDefinitions.AddOrUpdate(new FieldDefinition($"{Constants.Fields.SystemFieldPrefix}{Constants.Fields.SystemFields.SortOrder}", FieldDefinitionTypes.FullText));
        options.FieldDefinitions.AddOrUpdate(new FieldDefinition($"{Constants.Fields.SystemFieldPrefix}{Constants.Fields.SystemFields.Name}", FieldDefinitionTypes.FullTextSortable));
    }

    private void AddOptions(LuceneDirectoryIndexOptions options)
    {
        AddSystemFields(options);
        foreach (FieldOptions.Field field in _fieldOptions.Fields)
        {
            var fieldPostfix = field.FieldValues switch
            {
                FieldValues.Texts => Constants.Fields.Texts,
                FieldValues.TextsR1 => Constants.Fields.TextsR1,
                FieldValues.TextsR2 => Constants.Fields.TextsR2,
                FieldValues.TextsR3 => Constants.Fields.TextsR3,
                FieldValues.Integers => Constants.Fields.Integers,
                FieldValues.Decimals => Constants.Fields.Decimals,
                FieldValues.DateTimeOffsets => Constants.Fields.DateTimeOffsets,
                FieldValues.Keywords => Constants.Fields.Keywords,
                _ => throw new ArgumentOutOfRangeException(nameof(field.FieldValues))
            };

            var fieldDefinitionType = field.FieldValues switch
            {
                FieldValues.Texts or FieldValues.TextsR1 or FieldValues.TextsR2 or FieldValues.TextsR3 or FieldValues.Keywords
                    => field is { Sortable: true, Facetable: true }
                        ? FieldDefinitionTypes.FacetFullTextSortable
                        : field.Facetable
                            ? FieldDefinitionTypes.FacetFullText
                            : field.Sortable
                                ? FieldDefinitionTypes.FullTextSortable
                                : FieldDefinitionTypes.FullText,
                FieldValues.Integers => field.Facetable
                    ? FieldDefinitionTypes.FacetInteger
                    : FieldDefinitionTypes.Integer,
                FieldValues.Decimals => field.Facetable
                    ? FieldDefinitionTypes.FacetDouble
                    : FieldDefinitionTypes.Double,
                FieldValues.DateTimeOffsets => field.Facetable
                    ? FieldDefinitionTypes.FacetDateTime
                    : FieldDefinitionTypes.DateTime,
                _ => throw new ArgumentOutOfRangeException(nameof(field.FieldValues))
            };

            var fieldName = $"{Constants.Fields.FieldPrefix}{field.PropertyName}_{fieldPostfix}";
            // options.FacetsConfig.SetMultiValued(fieldName, true);
            options.FieldDefinitions.AddOrUpdate(new FieldDefinition(fieldName, fieldDefinitionType));
        }
    }
}
