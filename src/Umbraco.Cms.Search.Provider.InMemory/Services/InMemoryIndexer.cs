using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Provider.InMemory.Models;

namespace Umbraco.Cms.Search.Provider.InMemory.Services;

internal sealed class InMemoryIndexer : IInMemoryIndexer
{
    private readonly DataStore _dataStore;

    public InMemoryIndexer(DataStore dataStore)
        => _dataStore = dataStore;

    public Task AddOrUpdateAsync(string indexAlias, Guid id, UmbracoObjectTypes objectType, IEnumerable<Variation> variations, IEnumerable<IndexField> fields, ContentProtection? protection)
    {
        Remove(indexAlias, id);
        GetIndex(indexAlias)[id] = new IndexDocument(objectType, variations.ToArray(), fields.ToArray(), protection);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string indexAlias, IEnumerable<Guid> ids)
    {
        Guid[] keysArray = ids as Guid[] ?? ids.ToArray();

        // index is responsible for deleting descendants!
        foreach (Guid key in keysArray)
        {
            Remove(indexAlias, key);
            IEnumerable<Guid> descendantKeys = GetIndex(indexAlias).Where(v =>
                    v.Value.Fields.Any(f => f.FieldName == Constants.FieldNames.PathIds && f.Value.Keywords?.Contains($"{key:D}") is true))
                .Select(pair => pair.Key);
            foreach (Guid descendantKey in descendantKeys)
            {
                Remove(indexAlias, descendantKey);
            }
        }

        return Task.CompletedTask;
    }

    public Task ResetAsync(string indexAlias)
    {
        _dataStore.Remove(indexAlias);
        return Task.CompletedTask;
    }

    private void Remove(string index, Guid id)
    {
        GetIndex(index).Remove(id);
    }

    private Dictionary<Guid, IndexDocument> GetIndex(string indexAlias)
    {
        if (_dataStore.ContainsKey(indexAlias) is false)
        {
            _dataStore[indexAlias] = new();
        }

        return _dataStore[indexAlias];
    }
}
