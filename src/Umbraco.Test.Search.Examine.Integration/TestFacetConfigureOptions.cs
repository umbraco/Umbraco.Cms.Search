using Examine;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Search.Provider.Examine.Configuration;

namespace Umbraco.Test.Search.Examine.Integration;

public class TestFacetConfigureOptions : IConfigureOptions<FacetOptions>
{
    public void Configure(FacetOptions options)
    {
        options.Facets =
        [
            new FacetOptions.FacetEntry
            {
                PropertyName = "title",
                Values = ["texts"],
                FacetType = FieldDefinitionTypes.FacetFullText,
            },
            new FacetOptions.FacetEntry
            {
                PropertyName = "decimalproperty",
                Values = ["decimals"],
                FacetType = FieldDefinitionTypes.FacetDouble,
            }
        ];
    }
}