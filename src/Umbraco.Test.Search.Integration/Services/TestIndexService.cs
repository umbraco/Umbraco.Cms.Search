using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Services;

namespace Umbraco.Test.Search.Integration.Services;

public class TestIndexService : IIndexService
{
    private readonly Dictionary<string, Dictionary<Guid, TestIndexDocument>> _indexes = new();
        
    public Task AddOrUpdateAsync(string indexAlias, Guid id, UmbracoObjectTypes objectType, IEnumerable<Variation> variations, IEnumerable<IndexField> fields, ContentProtection? protection)
    {
        GetIndex(indexAlias)[id] = new (id, objectType, variations, fields, protection);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string indexAlias, IEnumerable<Guid> ids)
    {
        // index is responsible for deleting descendants
        foreach (var key in ids)
        {
            GetIndex(indexAlias).Remove(key);
            var descendantDocuments = GetIndex(indexAlias).Values.Where(document =>
                document.Fields.Any(f => f.FieldName == Constants.FieldNames.PathIds && f.Value.Keywords?.Contains($"{key:D}") is true)
            );
            foreach (var descendantDocument in descendantDocuments)
            {
                GetIndex(indexAlias).Remove(descendantDocument.Id);
            }
        }

        return Task.CompletedTask;
    }

    public IReadOnlyList<TestIndexDocument> Dump(string indexAlias) => GetIndex(indexAlias).Values.ToList();

    public void Reset() => _indexes.Clear();
    
    private Dictionary<Guid, TestIndexDocument> GetIndex(string index)
    {
        if (_indexes.ContainsKey(index) is false)
        {
            _indexes[index] = new();
        }

        return _indexes[index];
    }
}