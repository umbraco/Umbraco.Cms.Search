using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Configuration;
using Umbraco.Cms.Search.Core.Services;

namespace Umbraco.Cms.Search.Core.Configuration;

public sealed class IndexOptions
{
    private readonly Dictionary<string, IndexRegistration> _register = [];

    public void RegisterIndex<TIndexer, TSearcher, TIndexRebuildStrategy>(string indexAlias, params UmbracoObjectTypes[] containedObjectTypes)
        where TIndexer : class, IIndexer
        where TSearcher : class, ISearcher
        where TIndexRebuildStrategy : class, IIndexRebuildStrategy
    {
        ArgumentException.ThrowIfNullOrEmpty("Index alias cannot be empty", nameof(indexAlias));
        if (containedObjectTypes.Length is 0)
        {
            throw new ArgumentException($"Index \"{indexAlias}\" must define at least one contained object type",  nameof(containedObjectTypes));
        }

        _register[indexAlias] = new IndexRegistration(indexAlias, containedObjectTypes.Distinct(), typeof(TIndexer), typeof(TSearcher), typeof(TIndexRebuildStrategy));
    }

    public IndexRegistration[] GetIndexRegistrations()
        => _register.Values.ToArray();

    public IndexRegistration? GetIndexRegistration(string indexAlias)
        => _register.TryGetValue(indexAlias, out IndexRegistration? indexRegistration) ? indexRegistration : null;
}
