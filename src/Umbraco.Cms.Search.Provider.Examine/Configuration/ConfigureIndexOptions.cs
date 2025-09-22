﻿using Examine;
using Examine.Lucene;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Search.Provider.Examine.Helpers;
using CoreConstants = Umbraco.Cms.Search.Core.Constants;

namespace Umbraco.Cms.Search.Provider.Examine.Configuration;

internal sealed class ConfigureIndexOptions : IConfigureNamedOptions<LuceneDirectoryIndexOptions>
{
    private readonly FieldOptions _fieldOptions;

    public ConfigureIndexOptions(IOptions<FieldOptions> options)
        => _fieldOptions = options.Value;

    public void Configure(string? name, LuceneDirectoryIndexOptions options)
        => AddOptions(options);

    public void Configure(LuceneDirectoryIndexOptions options)
        => Configure(string.Empty, options);

    private void AddOptions(LuceneDirectoryIndexOptions options)
    {
        AddFields(options, ExamineSystemFieldsOptions(), (field, _) => field.PropertyName);
        AddFields(options, CoreSystemFieldsOptions(), (field, fieldValues) => FieldNameHelper.FieldName(field.PropertyName, fieldValues));
        AddFields(options, _fieldOptions.Fields, (field, fieldValues) => FieldNameHelper.FieldName(field.PropertyName, fieldValues));
    }

    private void AddFields(LuceneDirectoryIndexOptions options, FieldOptions.Field[] fields, Func<FieldOptions.Field, string, string> getFieldName)
    {
        foreach (FieldOptions.Field field in fields)
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
                FieldValues.Keywords => FieldDefinitionTypes.Raw,
                FieldValues.Texts or FieldValues.TextsR1 or FieldValues.TextsR2 or FieldValues.TextsR3
                    => FullTextDefinition(field),
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

            var fieldName = getFieldName(field, fieldValues);
            var facetFieldName = fieldName;
            options.FieldDefinitions.AddOrUpdate(new FieldDefinition(fieldName, fieldDefinitionType));

            if (field.FieldValues is FieldValues.Keywords && (field.Sortable || field.Facetable))
            {
                // add extra field for keyword field sorting and/or faceting
                var queryableKeywordFieldName = FieldNameHelper.QueryableKeywordFieldName(fieldName);
                options.FieldDefinitions.AddOrUpdate(new FieldDefinition(queryableKeywordFieldName, FullTextDefinition(field)));
                facetFieldName = queryableKeywordFieldName;
            }

            if (field.Facetable)
            {
                options.FacetsConfig.SetMultiValued(facetFieldName, true);
            }
        }
    }

    private string FullTextDefinition(FieldOptions.Field field)
        => field is { Sortable: true, Facetable: true }
                ? FieldDefinitionTypes.FacetFullTextSortable
                : field.Facetable
                    ? FieldDefinitionTypes.FacetFullText
                    : field.Sortable
                        ? FieldDefinitionTypes.FullTextSortable
                        : FieldDefinitionTypes.FullText;

    private FieldOptions.Field[] CoreSystemFieldsOptions()
        =>
        [
            new() { PropertyName = CoreConstants.FieldNames.Id, FieldValues = FieldValues.Keywords },
            new() { PropertyName = CoreConstants.FieldNames.ParentId, FieldValues = FieldValues.Keywords },
            new() { PropertyName = CoreConstants.FieldNames.PathIds, FieldValues = FieldValues.Keywords },
            new() { PropertyName = CoreConstants.FieldNames.ContentTypeId, FieldValues = FieldValues.Keywords },
            new() { PropertyName = CoreConstants.FieldNames.CreateDate, FieldValues = FieldValues.DateTimeOffsets },
            new() { PropertyName = CoreConstants.FieldNames.UpdateDate, FieldValues = FieldValues.DateTimeOffsets },
            new() { PropertyName = CoreConstants.FieldNames.Level, FieldValues = FieldValues.Integers },
            new() { PropertyName = CoreConstants.FieldNames.ObjectType, FieldValues = FieldValues.Keywords },
            new() { PropertyName = CoreConstants.FieldNames.SortOrder, FieldValues = FieldValues.Integers },
            new() { PropertyName = CoreConstants.FieldNames.Name, FieldValues = FieldValues.TextsR1, Sortable = true },
            new() { PropertyName = CoreConstants.FieldNames.Tags, FieldValues = FieldValues.Keywords },
        ];

    private FieldOptions.Field[] ExamineSystemFieldsOptions()
        =>
        [
            new() { PropertyName = Constants.SystemFields.Protection, FieldValues = FieldValues.Keywords },
            new() { PropertyName = Constants.SystemFields.Culture, FieldValues = FieldValues.Keywords },
            new() { PropertyName = Constants.SystemFields.Segment, FieldValues = FieldValues.Keywords },
            new() { PropertyName = Constants.SystemFields.AggregatedTexts, FieldValues = FieldValues.Texts },
            new() { PropertyName = Constants.SystemFields.AggregatedTextsR1, FieldValues = FieldValues.Texts },
            new() { PropertyName = Constants.SystemFields.AggregatedTextsR2, FieldValues = FieldValues.Texts },
            new() { PropertyName = Constants.SystemFields.AggregatedTextsR3, FieldValues = FieldValues.Texts },
        ];
}
