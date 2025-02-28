using Umbraco.Cms.Search.Core;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Services;
using Umbraco.Cms.Search.Provider.InMemory.Models;

namespace Umbraco.Cms.Search.Provider.InMemory.Services;

internal sealed class IndexService : IIndexService
{
    private readonly DataStore _index;

    public IndexService(DataStore index)
        => _index = index;

    public Task AddOrUpdateAsync(Guid key, string stamp, IEnumerable<Variation> variations, IEnumerable<IndexField> fields)
    {
        Remove(key);
        _index[key] = new IndexDocument(
            variations.ToArray(),
            fields
                .Union([new IndexField("Umb_DocumentStamp", new IndexValue { Keywords = [stamp] }, null, null)])
                .ToArray()
        );
        return Task.CompletedTask;
    }

    public Task DeleteAsync(IEnumerable<Guid> keys)
    {
        var keysArray = keys as Guid[] ?? keys.ToArray();

        // index is responsible for deleting descendants!
        foreach (var key in keysArray)
        {
            Remove(key);
            var descendantKeys = _index.Where(v =>
                    v.Value.Fields.Any(f => f.FieldName == IndexConstants.FieldNames.PathIds && f.Value.Keywords?.Contains($"{key:D}") is true)
                )
                .Select(pair => pair.Key);
            foreach (var descendantKey in descendantKeys)
            {
                Remove(descendantKey);
            }
        }

        return Task.CompletedTask;
    }

    private void Remove(Guid key)
    {
        _index.Remove(key);
    }
    
    public Task<string?> GetStampAsync(Guid key)
    {
        return Task.FromResult(
            _index.TryGetValue(key, out var document)
                ? document
                    .Fields
                    .Single(f => f.FieldName == "Umb_DocumentStamp")
                    .Value
                    .Keywords!
                    .First()
                : null
        );
    }
}