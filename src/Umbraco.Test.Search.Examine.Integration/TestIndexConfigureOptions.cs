using Examine;
using Microsoft.Extensions.Options;
using IndexOptions = Umbraco.Cms.Search.Provider.Examine.Configuration.IndexOptions;

namespace Umbraco.Test.Search.Examine.Integration;

public class TestIndexConfigureOptions : IConfigureOptions<IndexOptions>
{
    public void Configure(IndexOptions options)
    {
        options.Fields =
        [
            new IndexOptions.Field
            {
                PropertyName = "title",
                Values = ["texts"],
                Type = FieldDefinitionTypes.FacetFullText,
            },
            new IndexOptions.Field
            {
                PropertyName = "decimalproperty",
                Values = ["decimals"],
                Type = FieldDefinitionTypes.FacetDouble,
            },            
            new IndexOptions.Field
            {
                PropertyName = "sortableTitle",
                Values = ["texts"],
                Type = FieldDefinitionTypes.FullTextSortable,
            },
        ];
    }
}