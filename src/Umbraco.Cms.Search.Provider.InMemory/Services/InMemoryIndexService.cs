using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Services;
using Umbraco.Cms.Search.Provider.InMemory.Models;

namespace Umbraco.Cms.Search.Provider.InMemory.Services;

internal sealed class InMemoryIndexService : IIndexService
{
    private readonly DataStore _dataStore;

    public InMemoryIndexService(DataStore dataStore)
        => _dataStore = dataStore;

    public Task AddOrUpdateAsync(string indexAlias, Guid key, UmbracoObjectTypes objectType, IEnumerable<Variation> variations, IEnumerable<IndexField> fields, ContentProtection? protection)
    {
        Remove(indexAlias, key);
        GetIndex(indexAlias)[key] = new IndexDocument(objectType, variations.ToArray(), fields.ToArray(), protection);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string indexAlias, IEnumerable<Guid> keys)
    {
        var keysArray = keys as Guid[] ?? keys.ToArray();

        // index is responsible for deleting descendants!
        foreach (var key in keysArray)
        {
            Remove(indexAlias, key);
            var descendantKeys = GetIndex(indexAlias).Where(v =>
                    v.Value.Fields.Any(f => f.FieldName == Constants.FieldNames.PathIds && f.Value.Keywords?.Contains($"{key:D}") is true)
                )
                .Select(pair => pair.Key);
            foreach (var descendantKey in descendantKeys)
            {
                Remove(indexAlias, descendantKey);
            }
        }

        return Task.CompletedTask;
    }

    private void Remove(string index, Guid key)
    {
        GetIndex(index).Remove(key);
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