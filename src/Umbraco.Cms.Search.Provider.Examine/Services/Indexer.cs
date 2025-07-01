using Examine;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Services;

namespace Umbraco.Cms.Search.Provider.Examine.Services;

public class Indexer : IIndexer
{
    private readonly IExamineManager _examineManager;

    public Indexer(IExamineManager examineManager)
    {
        _examineManager = examineManager;
    }
    public Task AddOrUpdateAsync(string indexAlias, Guid key, UmbracoObjectTypes objectType, IEnumerable<Variation> variations,
        IEnumerable<IndexField> fields, ContentProtection? protection)
    {
        var index = GetIndex(indexAlias);

        DeleteSingleDoc(index, key);

        var valuesToIndex = new List<ValueSet>();

        foreach (var variation in variations)
        {
            var indexKey = CalculateIndexKey(key, variation);
            valuesToIndex.Add(new ValueSet(
                indexKey,
                objectType.ToString(),
                MapToDictionary(fields.Where(x => (x.FieldName.StartsWith("Umb_") && x.Culture is null && x.Segment is null) || (x.Culture == variation.Culture && x.Segment == variation.Segment)), protection)));
        }
        
        index.IndexItems(valuesToIndex);
        return Task.CompletedTask;
    }

    private string CalculateIndexKey(Guid key, Variation variation)
    {
        string result = key.ToString().ToLowerInvariant();

        if (variation.Culture is not null)
        {
            result += $"_{variation.Culture}";
        }
        if (variation.Segment is not null)
        {
            result += $"_{variation.Segment}";
        }
        
        return result;
    }

    private void DeleteSingleDoc(IIndex index, Guid key)
    {
        var documents = index.Searcher.CreateQuery().Field("Umb_Id_keywords", key.ToString()).Execute();
        
        var idsToDelete = new HashSet<string>();
        
        foreach (var document in documents)
        {
            idsToDelete.Add(document.Id);
        }

        if (idsToDelete.Any())
        {
            index.DeleteFromIndex(idsToDelete);
        }
    }
    
    
    public async Task DeleteAsync(string indexAlias, IEnumerable<Guid> keys)
    {
        var index = GetIndex(indexAlias);
        var idsToDelete = new HashSet<string>();
        
        foreach (var key in keys)
        {
            var documents = index.Searcher.Search(key.ToString());
            foreach (var document in documents)
            {
                idsToDelete.Add(document.Id);
            }
            
            var descendants = index.Searcher.CreateQuery().Field("Umb_PathIds_keywords", key.ToString()).Execute();
            
            foreach (var descendant in descendants)
            {
                idsToDelete.Add(descendant.Id);
            }
            
            index.DeleteFromIndex(idsToDelete);
        }
    }

    public Task ResetAsync(string indexAlias)
    {
        _examineManager.TryGetIndex(indexAlias, out var index);
        index?.CreateIndex();
        return Task.CompletedTask;
    }


    private Dictionary<string, object> MapToDictionary(IEnumerable<IndexField> fields, ContentProtection? protection)
    {
        var result = new Dictionary<string, object>();
        List<string> aggregatedTexts = [];
        foreach (var field in fields)
        {
            if (field.Value.Integers?.Any() ?? false)
            {
                result.Add(CalculateFieldName(field, "integers"), field.Value.Integers);
            }
            
            if (field.Value.Keywords?.Any() ?? false)
            {
                result.Add(CalculateFieldName(field, "keywords"), field.Value.Keywords);
                aggregatedTexts.AddRange(field.Value.Keywords);
            }
            
            if (field.Value.Decimals?.Any() ?? false)
            {
                result.Add(CalculateFieldName(field, "decimals"), field.Value.Decimals);
            }
            
            if (field.Value.DateTimeOffsets?.Any() ?? false)
            {
                result.Add(CalculateFieldName(field, "datetimeoffsets"), field.Value.DateTimeOffsets);
            }
            if (field.Value.Texts?.Any() ?? false)
            {
                result.Add(CalculateFieldName(field, "texts"), field.Value.Texts);
                aggregatedTexts.AddRange(field.Value.Texts);
            }
        }
        
        if (aggregatedTexts.Any())
        {
            result.Add("aggregated_texts", aggregatedTexts.ToArray());
        }

        if (protection is not null)
        {
            result.Add("protection", protection.AccessIds);
        }

        return result;
    }

    private string CalculateFieldName(IndexField field, string property)
    {
        var result = field.FieldName;
        return result + $"_{property}";
    }

    private IIndex GetIndex(string indexName)
    {
        if (_examineManager.TryGetIndex(indexName, out var index) is false)
        {
            throw new Exception($"The index {indexName} could not be found.");
        }

        return index;
    }
}