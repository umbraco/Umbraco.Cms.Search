using Examine;
using Microsoft.Extensions.Options;
using IndexOptions = Umbraco.Cms.Search.Provider.Examine.Configuration.IndexOptions;

namespace Umbraco.Test.Search.Examine.Integration;

public class TestIndexConfigureOptions : IConfigureOptions<IndexOptions>
{
    public void Configure(IndexOptions options)
    {
        options.Entries =
        [
            new IndexOptions.Entry
            {
                PropertyName = "title",
                Values = ["texts"],
                Type = FieldDefinitionTypes.FacetFullText,
            },
            new IndexOptions.Entry
            {
                PropertyName = "decimalproperty",
                Values = ["decimals"],
                Type = FieldDefinitionTypes.FacetDouble,
            },            
            new IndexOptions.Entry
            {
                PropertyName = "sortableTitle",
                Values = ["texts"],
                Type = FieldDefinitionTypes.FullTextSortable,
            },
        ];
    }
}