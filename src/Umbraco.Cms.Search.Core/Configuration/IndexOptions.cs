using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Configuration;
using Umbraco.Cms.Search.Core.Services;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;

namespace Umbraco.Cms.Search.Core.Configuration;

public sealed class IndexOptions
{
    private readonly Dictionary<string, IndexRegistration> _register = [];

    public void RegisterIndex<TIndexer, TSearcher, TContentChangeStrategy>(string indexAlias, params UmbracoObjectTypes[] containedObjectTypes)
        where TIndexer : class, IIndexer
        where TSearcher : class, ISearcher
        where TContentChangeStrategy : class, IContentChangeStrategy
    {
        ArgumentException.ThrowIfNullOrEmpty("Index alias cannot be empty", nameof(indexAlias));
        if (containedObjectTypes.Length is 0)
        {
            throw new ArgumentException($"Index \"{indexAlias}\" must define at least one contained object type",  nameof(containedObjectTypes));
        }

        _register[indexAlias] = new IndexRegistration(indexAlias, containedObjectTypes.Distinct(), typeof(TIndexer), typeof(TSearcher), typeof(TContentChangeStrategy));
    }

    public IndexRegistration[] GetIndexRegistrations()
        => _register.Values.ToArray();

    public IndexRegistration? GetIndexRegistration(string indexAlias)
        => _register.TryGetValue(indexAlias, out IndexRegistration? indexRegistration) ? indexRegistration : null;
}
