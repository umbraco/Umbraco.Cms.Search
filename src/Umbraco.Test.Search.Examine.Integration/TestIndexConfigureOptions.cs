using Microsoft.Extensions.Options;
using Umbraco.Cms.Search.Provider.Examine.Configuration;

namespace Umbraco.Test.Search.Examine.Integration;

public class TestIndexConfigureOptions : IConfigureOptions<FacetOptions>
{
    public void Configure(FacetOptions facetOptions)
        => facetOptions.Fields =
        [
            new FacetOptions.Field
            {
                PropertyName = "fieldSingleValues",
                FieldValues = FieldValues.Texts,
                Facetable = true,
            },
            new FacetOptions.Field
            {
                PropertyName = "fieldSingleValues",
                FieldValues = FieldValues.TextsR1,
                Facetable = true,
            },
            new FacetOptions.Field
            {
                PropertyName = "fieldSingleValues",
                FieldValues = FieldValues.TextsR2,
                Facetable = true,
            },
            new FacetOptions.Field
            {
                PropertyName = "fieldSingleValues",
                FieldValues = FieldValues.TextsR3,
                Facetable = true,
            },
            new FacetOptions.Field
            {
                PropertyName = "fieldSingleValues",
                FieldValues = FieldValues.Integers,
                Facetable = true,
            },
            new FacetOptions.Field
            {
                PropertyName = "fieldSingleValues",
                FieldValues = FieldValues.Decimals,
                Facetable = true,
            },
            new FacetOptions.Field
            {
                PropertyName = "fieldSingleValues",
                FieldValues = FieldValues.DateTimeOffsets,
                Facetable = true,
            },
            // TODO KJA: why are we registering these? shouldn't this work out of the box? something to do with multivalue, perhaps?
            new FacetOptions.Field
            {
                PropertyName = "fieldMultipleValues",
                FieldValues = FieldValues.Integers,
            },
            new FacetOptions.Field
            {
                PropertyName = "fieldMultipleValues",
                FieldValues = FieldValues.Decimals,
            },
            new FacetOptions.Field
            {
                PropertyName = "fieldMultipleValues",
                FieldValues = FieldValues.DateTimeOffsets,
            },
            new FacetOptions.Field
            {
                PropertyName = "fieldone",
                FieldValues = FieldValues.Integers,
            },
            new FacetOptions.Field
            {
                PropertyName = "title",
                FieldValues = FieldValues.Texts,
                Facetable = true,
            },
            new FacetOptions.Field
            {
                PropertyName = "dropDown",
                FieldValues = FieldValues.Keywords,
                Facetable = true,
            },
            new FacetOptions.Field
            {
                PropertyName = "decimalproperty",
                FieldValues = FieldValues.Decimals,
                Facetable = true,
            },
            new FacetOptions.Field
            {
                PropertyName = "sortableTitle",
                FieldValues = FieldValues.Texts,
                Sortable = true,
            },
            new FacetOptions.Field
            {
                PropertyName = "datetime",
                FieldValues = FieldValues.DateTimeOffsets,
                Facetable = true,
            },
            new FacetOptions.Field
            {
                PropertyName = "count",
                FieldValues = FieldValues.Integers,
                Facetable = true,
            },
        ];
}
