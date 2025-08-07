using Examine;
using Examine.Lucene;
using Microsoft.Extensions.Options;

namespace Umbraco.Cms.Search.Provider.Examine.Configuration;

public sealed class ConfigureIndexOptions : IConfigureNamedOptions<LuceneDirectoryIndexOptions>
{
    private readonly IOptions<FacetOptions> _facetOptions;

    public ConfigureIndexOptions(IOptions<FacetOptions> facetOptions)
    {
        _facetOptions = facetOptions;
    }
    public void Configure(string name, LuceneDirectoryIndexOptions options)
    {
        AddOptions(options);
    }

    public void Configure(LuceneDirectoryIndexOptions options) 
        => Configure(string.Empty, options);

    private void AddOptions(LuceneDirectoryIndexOptions options)
    {
        foreach (var facetEntry in _facetOptions.Value.Fields)
        {
            foreach (var propertyIndexValue in facetEntry.Values)
            {
                var fieldName = $"Umb_{facetEntry.PropertyName}_{propertyIndexValue}";
                // options.FacetsConfig.SetMultiValued(fieldName, true);
                options.FieldDefinitions.AddOrUpdate(new FieldDefinition(fieldName, facetEntry.Type));
            }
        }
    }
}