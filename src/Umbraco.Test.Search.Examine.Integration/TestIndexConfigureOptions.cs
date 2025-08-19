﻿using Microsoft.Extensions.Options;
using Umbraco.Cms.Search.Provider.Examine.Configuration;

namespace Umbraco.Test.Search.Examine.Integration;

public class TestIndexConfigureOptions : IConfigureOptions<FieldOptions>
{
    public void Configure(FieldOptions fieldOptions)
        => fieldOptions.Fields =
        [
            new FieldOptions.Field
            {
                PropertyName = "fieldSingleValues",
                FieldValues = FieldValues.Texts,
                Facetable = true,
            },
            new FieldOptions.Field
            {
                PropertyName = "fieldSingleValues",
                FieldValues = FieldValues.Keywords,
                Facetable = true,
            },
            new FieldOptions.Field
            {
                PropertyName = "fieldSingleValues",
                FieldValues = FieldValues.TextsR1,
                Facetable = true,
            },
            new FieldOptions.Field
            {
                PropertyName = "fieldSingleValues",
                FieldValues = FieldValues.TextsR2,
                Facetable = true,
            },
            new FieldOptions.Field
            {
                PropertyName = "fieldSingleValues",
                FieldValues = FieldValues.TextsR3,
                Facetable = true,
            },
            new FieldOptions.Field
            {
                PropertyName = "fieldSingleValues",
                FieldValues = FieldValues.Integers,
                Facetable = true,
            },
            new FieldOptions.Field
            {
                PropertyName = "fieldSingleValues",
                FieldValues = FieldValues.Decimals,
                Facetable = true,
            },
            new FieldOptions.Field
            {
                PropertyName = "fieldSingleValues",
                FieldValues = FieldValues.DateTimeOffsets,
                Facetable = true,
            },
            new FieldOptions.Field
            {
                PropertyName = "title",
                FieldValues = FieldValues.Texts,
                Facetable = true,
            },
            new FieldOptions.Field
            {
                PropertyName = "dropDown",
                FieldValues = FieldValues.Keywords,
                Facetable = true,
            },
            new FieldOptions.Field
            {
                PropertyName = "decimalproperty",
                FieldValues = FieldValues.Decimals,
                Facetable = true,
            },
            new FieldOptions.Field
            {
                PropertyName = "sortableTitle",
                FieldValues = FieldValues.Texts,
                Sortable = true,
            },
            new FieldOptions.Field
            {
                PropertyName = "datetime",
                FieldValues = FieldValues.DateTimeOffsets,
                Facetable = true,
            },
            new FieldOptions.Field
            {
                PropertyName = "count",
                FieldValues = FieldValues.Integers,
                Facetable = true,
            },
            // for the time being, we need to register filterable, non-textual fields explicitly due to complications with multivalue fields
            new FieldOptions.Field
            {
                PropertyName = "fieldMultipleValues",
                FieldValues = FieldValues.Integers,
            },
            new FieldOptions.Field
            {
                PropertyName = "fieldMultipleValues",
                FieldValues = FieldValues.Decimals,
            },
            new FieldOptions.Field
            {
                PropertyName = "fieldMultipleValues",
                FieldValues = FieldValues.DateTimeOffsets,
            },
            new FieldOptions.Field
            {
                PropertyName = "fieldMultipleValues",
                FieldValues = FieldValues.Keywords,
            },
            new FieldOptions.Field
            {
                PropertyName = "fieldone",
                FieldValues = FieldValues.Integers,
            },
        ];
}
