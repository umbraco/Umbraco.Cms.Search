using Examine;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Search.Provider.Examine;
using Umbraco.Cms.Search.Provider.Examine.Configuration;

namespace Umbraco.Test.Search.Examine.Integration;

public class TestIndexConfigureOptions : IConfigureOptions<FacetOptions>
{
    public void Configure(FacetOptions facetOptions)
    {
        facetOptions.Fields =
        [
            new FacetOptions.Field
            {
                PropertyName = "fieldSingleValues",
                Values = [Constants.Fields.Texts],
                Type = FieldDefinitionTypes.FacetFullText,
            },
            new FacetOptions.Field
            {
                PropertyName = "fieldSingleValues",
                Values = [Constants.Fields.TextsR1],
                Type = FieldDefinitionTypes.FacetFullText,
            },
            new FacetOptions.Field
            {
                PropertyName = "fieldSingleValues",
                Values = [Constants.Fields.TextsR2],
                Type = FieldDefinitionTypes.FacetFullText,
            },
            new FacetOptions.Field
            {
                PropertyName = "fieldSingleValues",
                Values = [Constants.Fields.TextsR3],
                Type = FieldDefinitionTypes.FacetFullText,
            },
            new FacetOptions.Field
            {
                PropertyName = "fieldSingleValues",
                Values = [Constants.Fields.Integers],
                Type = FieldDefinitionTypes.FacetInteger,
            },
            new FacetOptions.Field
            {
                PropertyName = "fieldSingleValues",
                Values = [Constants.Fields.Decimals],
                Type = FieldDefinitionTypes.FacetDouble,
            },
            new FacetOptions.Field
            {
                PropertyName = "fieldSingleValues",
                Values = [Constants.Fields.DateTimeOffsets],
                Type = FieldDefinitionTypes.FacetDateTime,
            },
            new FacetOptions.Field
            {
                PropertyName = "fieldMultipleValues",
                Values = [Constants.Fields.Integers],
                Type = FieldDefinitionTypes.Integer,
            },
            new FacetOptions.Field
            {
                PropertyName = "fieldMultipleValues",
                Values = [Constants.Fields.Decimals],
                Type = FieldDefinitionTypes.Double,
            },
            new FacetOptions.Field
            {
                PropertyName = "fieldMultipleValues",
                Values = [Constants.Fields.DateTimeOffsets],
                Type = FieldDefinitionTypes.DateTime,
            },
            new FacetOptions.Field
            {
                PropertyName = "fieldone",
                Values = [Constants.Fields.Integers],
                Type = FieldDefinitionTypes.Integer,
            },
            new FacetOptions.Field
            {
                PropertyName = "title",
                Values = [Constants.Fields.Texts, Constants.Fields.Keywords],
                Type = FieldDefinitionTypes.FacetFullText,
            },
            new FacetOptions.Field
            {
                PropertyName = "ContentTypeId",
                Values = [Constants.Fields.Keywords],
                Type = FieldDefinitionTypes.FacetFullText,
            },
            new FacetOptions.Field
            {
                PropertyName = "decimalproperty",
                Values = [Constants.Fields.Decimals],
                Type = FieldDefinitionTypes.FacetDouble,
            },
            new FacetOptions.Field
            {
                PropertyName = "sortableTitle",
                Values = [Constants.Fields.Texts],
                Type = FieldDefinitionTypes.FullTextSortable,
            },
            new FacetOptions.Field
            {
                PropertyName = "datetime",
                Values = [Constants.Fields.DateTimeOffsets],
                Type = FieldDefinitionTypes.FacetDateTime,
            },
            new FacetOptions.Field
            {
                PropertyName = "count",
                Values = [Constants.Fields.Integers],
                Type = FieldDefinitionTypes.FacetInteger,
            },
        ];
    }
}
