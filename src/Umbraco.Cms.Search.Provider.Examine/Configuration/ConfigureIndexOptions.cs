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
        switch (name)
        {
            case Cms.Search.Core.Constants.IndexAliases.DraftContent:
                AddOptions(options);
                break;
            case Cms.Search.Core.Constants.IndexAliases.PublishedContent:
                AddOptions(options);
                break;
            case Cms.Search.Core.Constants.IndexAliases.DraftMembers:
                AddOptions(options);
                break;
            case Cms.Search.Core.Constants.IndexAliases.DraftMedia:
                AddOptions(options);
                break;
        }
    }

    public void Configure(LuceneDirectoryIndexOptions options) 
        => Configure(string.Empty, options);

    private void AddOptions(LuceneDirectoryIndexOptions options)
    {
        foreach (var facetEntry in _facetOptions.Value.Fields)
        {
            foreach (var propertyIndexValue in facetEntry.Values)
            {
                options.FieldDefinitions.AddOrUpdate(new FieldDefinition($"Umb_{facetEntry.PropertyName}_{propertyIndexValue}", facetEntry.Type));
            }
        }
    }
}