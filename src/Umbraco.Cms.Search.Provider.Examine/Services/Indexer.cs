using Examine;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Provider.Examine.Extensions;

namespace Umbraco.Cms.Search.Provider.Examine.Services;

public class Indexer(IExamineManager examineManager, IDefaultCultureAccessor defaultCultureAccessor, ILogger<Indexer> logger) : IExamineIndexer
{
    public Task AddOrUpdateAsync(string indexAlias, Guid key, UmbracoObjectTypes objectType, IEnumerable<Variation> variations,
        IEnumerable<IndexField> fields, ContentProtection? protection)
    {
        var index = GetIndex(indexAlias);

        DeleteSingleDoc(index, key);

        var valuesToIndex = new List<ValueSet>();

        foreach (Variation variation in variations)
        {
            var indexKey = CalculateIndexKey(key, variation);
            valuesToIndex.Add(new ValueSet(
                indexKey,
                objectType.ToString(),
                MapToDictionary(fields.Where(x => (x.Culture is null && x.Segment is null) || (x.Culture == variation.Culture && x.Segment == variation.Segment)), variation.Culture, variation.Segment, protection)));
        }

        index.IndexItems(valuesToIndex);

        return Task.CompletedTask;
    }

    public Task ResetAsync(string indexAlias)
    {
        if (examineManager.TryGetIndex(indexAlias, out var index) is false)
        {
            return Task.CompletedTask;
        }
        index?.CreateIndex();
        return Task.CompletedTask;
    }

    private static string CalculateIndexKey(Guid key, Variation variation)
    {
        var result = key.ToString().ToLowerInvariant();

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
        var documents = index.Searcher.CreateQuery().Field("Umb_Id_keywords", key.ToString().TransformDashes()).Execute();

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


    private Dictionary<string, IEnumerable<object>> MapToDictionary(IEnumerable<IndexField> fields, string? culture, string? segment, ContentProtection? protection)
    {
        var result = new Dictionary<string, IEnumerable<object>>();
        List<string> aggregatedTexts = [];
        List<string> aggregatedR1Texts = [];
        List<string> aggregatedR2Texts = [];
        List<string> aggregatedR3Texts = [];
        foreach (var field in fields)
        {
            if (field.FieldName.StartsWith(Constants.Fields.FieldPrefix) is false && (culture is not null || segment is not null))
            {
                if ((field.Culture != culture && culture != defaultCultureAccessor.DefaultCulture)  || field.Segment != segment)
                {
                    continue;
                }
            }
            if (field.Value.Integers?.Any() ?? false)
            {
                List<object> integers = field.Value.Integers.Cast<object>().ToList();
                result.Add(CalculateFieldName(field, Constants.Fields.Integers),  integers);
            }

            if (field.Value.Keywords?.Any() ?? false)
            {
                result.Add(CalculateFieldName(field, Constants.Fields.Keywords), field.Value.Keywords.Select(x => x.TransformDashes()));
                aggregatedTexts.AddRange(field.Value.Keywords);
            }

            if (field.Value.Decimals?.Any() ?? false)
            {
                List<object> decimals = field.Value.Decimals.Cast<object>().ToList();
                result.Add(CalculateFieldName(field, Constants.Fields.Decimals), decimals);
            }

            if (field.Value.DateTimeOffsets?.Any() ?? false)
            {
                // We have to use DateTime here, as examine facets does not play nice with DatetimeOffsets for now.
                List<object> dates = field.Value.DateTimeOffsets.Select(x => x.DateTime).Cast<object>().ToList();
                result.Add(CalculateFieldName(field, Constants.Fields.DateTimeOffsets), dates);
            }
            if (field.Value.Texts?.Any() ?? false)
            {
                result.Add(CalculateFieldName(field, Constants.Fields.Texts), field.Value.Texts.Select(x => x.TransformDashes()));
                aggregatedTexts.AddRange(field.Value.Texts);
            }

            if (field.Value.TextsR1?.Any() ?? false)
            {
                aggregatedR1Texts.AddRange(field.Value.TextsR1);
                result.Add(CalculateFieldName(field, Constants.Fields.TextsR1), field.Value.TextsR1.Select(x => x.TransformDashes()));
                aggregatedTexts.AddRange(field.Value.TextsR1);
            }

            if (field.Value.TextsR2?.Any() ?? false)
            {
                aggregatedR2Texts.AddRange(field.Value.TextsR2);
                result.Add(CalculateFieldName(field, Constants.Fields.TextsR2), field.Value.TextsR2.Select(x => x.TransformDashes()));
                aggregatedTexts.AddRange(field.Value.TextsR2);
            }

            if (field.Value.TextsR3?.Any() ?? false)
            {
                aggregatedR3Texts.AddRange(field.Value.TextsR3);
                result.Add(CalculateFieldName(field, Constants.Fields.TextsR3), field.Value.TextsR3.Select(x => x.TransformDashes()));
                aggregatedTexts.AddRange(field.Value.TextsR3);
            }
        }

        if (aggregatedTexts.Any())
        {
            result.Add($"{Constants.Fields.FieldPrefix}aggregated_texts", aggregatedTexts.ToArray());
        }

        if (aggregatedR1Texts.Any())
        {
            result.Add($"{Constants.Fields.FieldPrefix}_{Constants.Fields.TextsR1}", aggregatedR1Texts.ToArray());
        }
        if (aggregatedR2Texts.Any())
        {
            result.Add($"{Constants.Fields.FieldPrefix}_{Constants.Fields.TextsR2}", aggregatedR2Texts.ToArray());
        }
        if (aggregatedR3Texts.Any())
        {
            result.Add($"{Constants.Fields.FieldPrefix}_{Constants.Fields.TextsR3}", aggregatedR3Texts.ToArray());
        }


        // Cannot add null values, so we have to just say "none" here, so we can filter on variant / invariant content
        result.Add($"{Constants.Fields.FieldPrefix}{Constants.Fields.Culture}", [culture?.TransformDashes() ?? "none"]);
        result.Add($"{Constants.Fields.FieldPrefix}{Constants.Fields.Segment}", [segment?.TransformDashes() ?? "none"]);
        var protectionIds = protection?.AccessIds ?? new List<Guid> {Guid.Empty};
        result.Add($"{Constants.Fields.FieldPrefix}{Constants.Fields.Protection}", protectionIds.Select(x => x.ToString().TransformDashes()));

        return result;
    }

    private string CalculateFieldName(IndexField field, string property)
    {
        var result = $"{field.FieldName}";
        if (result.StartsWith(Constants.Fields.FieldPrefix) is false)
        {
            result = Constants.Fields.FieldPrefix + result;
        }
        return result + $"_{property}";
    }

    private IIndex GetIndex(string indexName)
    {
        if (examineManager.TryGetIndex(indexName, out var index) is false)
        {
            throw new Exception($"The index {indexName} could not be found.");
        }
        return index;
    }
}
