using Examine;
using Examine.Lucene;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Search.Provider.Examine.Extensions;
using Umbraco.Cms.Search.Provider.Examine.Helpers;
using CoreConstants = Umbraco.Cms.Search.Core.Constants;

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
        options.FieldDefinitions.AddOrUpdate(new FieldDefinition(FieldNameHelper.FieldName(CoreConstants.FieldNames.Id, Constants.FieldValues.Keywords), FieldDefinitionTypes.FullText));
        options.FieldDefinitions.AddOrUpdate(new FieldDefinition(FieldNameHelper.FieldName(CoreConstants.FieldNames.ParentId, Constants.FieldValues.Keywords), FieldDefinitionTypes.FullText));
        options.FieldDefinitions.AddOrUpdate(new FieldDefinition(FieldNameHelper.FieldName(CoreConstants.FieldNames.PathIds, Constants.FieldValues.Keywords), FieldDefinitionTypes.FullText));
        options.FieldDefinitions.AddOrUpdate(new FieldDefinition(FieldNameHelper.FieldName(CoreConstants.FieldNames.ContentTypeId, Constants.FieldValues.Keywords), FieldDefinitionTypes.FullText));
        options.FieldDefinitions.AddOrUpdate(new FieldDefinition(FieldNameHelper.FieldName(CoreConstants.FieldNames.CreateDate, Constants.FieldValues.DateTimeOffsets), FieldDefinitionTypes.DateTime));
        options.FieldDefinitions.AddOrUpdate(new FieldDefinition(FieldNameHelper.FieldName(CoreConstants.FieldNames.UpdateDate, Constants.FieldValues.DateTimeOffsets), FieldDefinitionTypes.DateTime));
        options.FieldDefinitions.AddOrUpdate(new FieldDefinition(FieldNameHelper.FieldName(CoreConstants.FieldNames.Level, Constants.FieldValues.Integers), FieldDefinitionTypes.Integer));
        options.FieldDefinitions.AddOrUpdate(new FieldDefinition(FieldNameHelper.FieldName(CoreConstants.FieldNames.ObjectType, Constants.FieldValues.Integers), FieldDefinitionTypes.Integer));
        options.FieldDefinitions.AddOrUpdate(new FieldDefinition(FieldNameHelper.FieldName(CoreConstants.FieldNames.SortOrder, Constants.FieldValues.Integers), FieldDefinitionTypes.FullText));
        options.FieldDefinitions.AddOrUpdate(new FieldDefinition(FieldNameHelper.FieldName(CoreConstants.FieldNames.Name, Constants.FieldValues.TextsR1), FieldDefinitionTypes.FullTextSortable));
    }

    private void AddOptions(LuceneDirectoryIndexOptions options)
    {
        AddSystemFields(options);
        foreach (FieldOptions.Field field in _fieldOptions.Fields)
        {
            var fieldValues = field.FieldValues switch
            {
                FieldValues.Texts => Constants.FieldValues.Texts,
                FieldValues.TextsR1 => Constants.FieldValues.TextsR1,
                FieldValues.TextsR2 => Constants.FieldValues.TextsR2,
                FieldValues.TextsR3 => Constants.FieldValues.TextsR3,
                FieldValues.Integers => Constants.FieldValues.Integers,
                FieldValues.Decimals => Constants.FieldValues.Decimals,
                FieldValues.DateTimeOffsets => Constants.FieldValues.DateTimeOffsets,
                FieldValues.Keywords => Constants.FieldValues.Keywords,
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

            var fieldName = FieldNameHelper.FieldName(field.PropertyName, fieldValues);
            // options.FacetsConfig.SetMultiValued(fieldName, true);
            options.FieldDefinitions.AddOrUpdate(new FieldDefinition(fieldName, fieldDefinitionType));

            if (field.FieldValues is FieldValues.Keywords && _fieldOptions.HasKeywordField(field.PropertyName))
            {
                // add RAW field for keyword filtering
                options.FieldDefinitions.AddOrUpdate(new FieldDefinition(fieldName.KeywordFieldName(), FieldDefinitionTypes.Raw));
            }
        }
    }
}
