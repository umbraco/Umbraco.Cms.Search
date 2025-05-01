using Examine;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Services;

namespace Umbraco.Cms.Search.Provider.Examine.Services;

public class IndexService : IIndexService
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
        
        index.IndexItem(new ValueSet(
            key.ToString(),
            objectType.ToString(),
            MapToDictionary(fields)));

        return Task.CompletedTask;
    }
    
    
    public Task DeleteAsync(string indexAlias, IEnumerable<Guid> keys)
    {
        var index = GetIndex(indexAlias);

        foreach (var key in keys)
        {
            index.DeleteFromIndex(key.ToString());
        }
        
        return Task.CompletedTask;
    }


    private Dictionary<string, object> MapToDictionary(IEnumerable<IndexField> fields)
    {
        return fields.ToDictionary<IndexField, string, object>(CalculateFieldName, field => MapIndexValue(field.Value));
    }

    private string CalculateFieldName(IndexField field)
    {
        var result = field.FieldName;
        if (field.Culture is not null)
        {
            result += $"_{field.Culture}";
        }

        if (field.Segment is not null)
        {
            result += $"_{field.Segment}";
        }

        return result;
    }

    private string MapIndexValue(IndexValue indexValue)
    {
        string result = string.Empty;

        if (indexValue.Texts is not null && indexValue.Texts.Any())
        {
            result += string.Join(",", indexValue.Texts);
        }

        if (indexValue.Keywords is not null && indexValue.Keywords.Any())
        {
            result += string.Join(",", indexValue.Keywords);
        }

        if (indexValue.Integers is not null && indexValue.Integers.Any())
        {
            result += string.Join(",", indexValue.Integers);
        }

        if (indexValue.Decimals is not null && indexValue.Decimals.Any())
        {
            result += string.Join(",", indexValue.Decimals);
        }

        if (indexValue.DateTimeOffsets is not null && indexValue.DateTimeOffsets.Any())
        {
            result += string.Join(",", indexValue.DateTimeOffsets);
        }

        return result;
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