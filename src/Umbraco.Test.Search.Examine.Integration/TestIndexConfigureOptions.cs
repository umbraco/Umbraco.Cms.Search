using Examine;
using Microsoft.Extensions.Options;
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
                PropertyName = "title",
                Values = ["texts", "keywords"],
                Type = FieldDefinitionTypes.FacetFullText,
            },
            new FacetOptions.Field
            {
                PropertyName = "decimalproperty",
                Values = ["decimals"],
                Type = FieldDefinitionTypes.FacetDouble,
            },            
            new FacetOptions.Field
            {
                PropertyName = "sortableTitle",
                Values = ["texts"],
                Type = FieldDefinitionTypes.FullTextSortable,
            },
            new FacetOptions.Field
            {
                PropertyName = "datetime",
                Values = ["datetimeoffsets"],
                Type = FieldDefinitionTypes.FacetDateTime,
            },
        ];
    }
}