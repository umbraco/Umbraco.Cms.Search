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
            MapToDictionary(fields, variations)));

        return Task.CompletedTask;
    }

    private Dictionary<string, object> MapToDictionary(IEnumerable<IndexField> fields, IEnumerable<Variation> variations)
    {
        var result = new Dictionary<string, object>();

        if (variations.Any() is false)
        {
            foreach (var field in fields)
            {
                result.Add(field.FieldName, MapIndexValue(field.Value));
            }
        }

        else
        {
            foreach (var variation in variations)
            {
                foreach (var field in fields.Where(x => x.Culture == variation.Culture))
                {
                    result.Add($"{field.FieldName}_{field.Culture}", MapIndexValue(field.Value));
                }
            }
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

    public Task DeleteAsync(string indexAlias, IEnumerable<Guid> keys)
    {
        throw new NotImplementedException();
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