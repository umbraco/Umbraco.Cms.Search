using Examine;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Services;
using Umbraco.Cms.Search.Provider.Examine.Configuration;

namespace Umbraco.Cms.Search.Provider.Examine.Services;

public class IndexService : IIndexer
{
    private readonly IExamineManager _examineManager;

    public IndexService(IExamineManager examineManager)
    {
        _examineManager = examineManager;
    }
    public Task AddOrUpdateAsync(string indexAlias, Guid key, UmbracoObjectTypes objectType, IEnumerable<Variation> variations,
        IEnumerable<IndexField> fields, ContentProtection? protection)
    {
        var index = GetIndex(indexAlias);

        foreach (var variation in variations)
        {
            var indexKey = CalculateIndexKey(key, variation);
            index.IndexItem(new ValueSet(
                indexKey,
                objectType.ToString(),
                MapToDictionary(fields.Where(x => (x.FieldName.StartsWith("Umb_") && x.Culture is null && x.Segment is null) || (x.Culture == variation.Culture && x.Segment == variation.Segment)), protection)));
        }

        return Task.CompletedTask;
    }

    private string CalculateIndexKey(Guid key, Variation variation)
    {
        string result = key.ToString().ToLowerInvariant();

        if (variation.Culture is not null)
        {
            result += $"-{variation.Culture}";
        }
        if (variation.Segment is not null)
        {
            result += $"_{variation.Segment}";
        }
        
        return result;
    }
    
    
    public async Task DeleteAsync(string indexAlias, IEnumerable<Guid> keys)
    {
        var index = GetIndex(indexAlias);

        foreach (var key in keys)
        {
            index.DeleteFromIndex(key.ToString());
            //TODO: Fix this, this should work, but searching in the index locks it, and thus we cannot delete.
            var results = index.Searcher.CreateQuery().Field("Umb_PathIds", key.ToString()).Execute();
            index.DeleteFromIndex(results.Select(x => x.Id.ToLowerInvariant()));
        }
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