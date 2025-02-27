using Package;
using Package.Models.Indexing;
using Package.Services;

namespace IntegrationTests.Services;

public class TestIndexService : IIndexService
{
    private readonly Dictionary<Guid, TestIndexDocument> _documents = new();
        
    public Task AddOrUpdateAsync(Guid key, string stamp, IEnumerable<Variation> variations, IEnumerable<IndexField> fields)
    {
        _documents[key] = new (key, stamp, variations, fields);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(IEnumerable<Guid> keys)
    {
        // index is responsible for deleting descendants
        foreach (var key in keys)
        {
            _documents.Remove(key);
            var descendantDocuments = _documents.Values.Where(document =>
                document.Fields.Any(f => f.FieldName == IndexConstants.FieldNames.AncestorIds && f.Value.Keywords?.Contains($"{key:D}") is true)
            );
            foreach (var descendantDocument in descendantDocuments)
            {
                _documents.Remove(descendantDocument.Key);
            }
        }

        return Task.CompletedTask;
    }

    public Task<string?> GetStampAsync(Guid key)
        => Task.FromResult(_documents.TryGetValue(key, out var document) ? document.Stamp : null);

    public IReadOnlyList<TestIndexDocument> Dump() => _documents.Values.ToList();

    public void Reset() => _documents.Clear();
}