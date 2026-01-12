using Examine;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Extensions;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Provider.Examine.Configuration;
using Umbraco.Cms.Search.Provider.Examine.Helpers;
using CoreConstants = Umbraco.Cms.Search.Core.Constants;

namespace Umbraco.Cms.Search.Provider.Examine.Services;

internal sealed class Indexer : IExamineIndexer
{
    private readonly IExamineManager _examineManager;
    private readonly FieldOptions _fieldOptions;

    public Indexer(IExamineManager examineManager, IOptions<FieldOptions> fieldOptions)
    {
        _examineManager = examineManager;
        _fieldOptions = fieldOptions.Value;
    }

    public Task AddOrUpdateAsync(
        string indexAlias,
        Guid key,
        UmbracoObjectTypes objectType,
        IEnumerable<Variation> variations,
        IEnumerable<IndexField> fields,
        ContentProtection? protection)
    {
        IIndex index = GetIndex(indexAlias);

        DeleteSingleDoc(index, key);

        var valuesToIndex = new List<ValueSet>();

        foreach (Variation variation in variations)
        {
            var indexKey = CalculateIndexKey(key, variation);
            IEnumerable<IndexField> fieldsToMap = MapFields(fields, variation);
            valuesToIndex.Add(new ValueSet(
                indexKey,
                objectType.ToString(),
                MapToDictionary(fieldsToMap, variation.Culture, variation.Segment, protection)));
        }

        index.IndexItems(valuesToIndex);

        return Task.CompletedTask;
    }

    private IEnumerable<IndexField> MapFields(IEnumerable<IndexField> fields, Variation variation)
    {
        var results = new Dictionary<string, IndexField>();
        foreach (IndexField field in fields)
        {
            if (field.Culture is null && field.Segment is null)
            {
                if (results.TryGetValue(field.FieldName, out IndexField? indexField))
                {
                    results[field.FieldName] = new IndexField(field.FieldName, MergeIndexValue(indexField.Value, field.Value), variation.Culture, variation.Segment);
                    continue;
                }

                results.Add(field.FieldName, field);
            }

            if (field.Culture == variation.Culture && field.Segment == variation.Segment)
            {
                if (results.TryGetValue(field.FieldName, out IndexField? indexField))
                {
                    results[field.FieldName] = new IndexField(field.FieldName, MergeIndexValue(indexField.Value, field.Value), variation.Culture, variation.Segment);
                    continue;
                }

                results[field.FieldName] = field;
            }
        }

        return results.Select(x => x.Value);
    }

    private IndexValue MergeIndexValue(IndexValue original, IndexValue toMerge) =>
        new()
        {
            Keywords = MergeValues(original.Keywords, toMerge.Keywords),
            Integers = MergeValues(original.Integers, toMerge.Integers),
            Decimals = MergeValues(original.Decimals, toMerge.Decimals),
            DateTimeOffsets = MergeValues(original.DateTimeOffsets, toMerge.DateTimeOffsets),
            Texts = MergeValues(original.Texts, toMerge.Texts),
            TextsR1 = MergeValues(original.TextsR1, toMerge.TextsR1),
            TextsR2 = MergeValues(original.TextsR2, toMerge.TextsR2),
            TextsR3 = MergeValues(original.TextsR3, toMerge.TextsR3),
        };

    private static IEnumerable<T>? MergeValues<T>(IEnumerable<T>? one, IEnumerable<T>? other)
    {
        IEnumerable<T>? list = one;
        if (list is null)
        {
            list = other;
        }
        else
        {
            if (other is not null)
            {
                return list.Concat(other).Distinct();
            }
        }

        return list;
    }

    public Task ResetAsync(string indexAlias)
    {
        // NOTE: the index might not exist at this point, so don't use GetIndex (it's throws an exception for non-existing indexes)
        if (_examineManager.TryGetIndex(indexAlias, out IIndex? index) is false)
        {
            return Task.CompletedTask;
        }
        index.CreateIndex();
        return Task.CompletedTask;
    }

    public async Task<long> GetDocumentCountAsync(string indexAlias)
    {
        if (_examineManager.TryGetIndex(indexAlias, out var index))
        {
            if (index is IIndexStats stats)
            {
                return stats.GetDocumentCount();
            }
        }

        return 0;
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
        ISearchResults documents = index.Searcher.CreateQuery().Field(FieldNameHelper.FieldName(CoreConstants.FieldNames.Id, Constants.FieldValues.Keywords), key.AsKeyword()).Execute();

        var idsToDelete = new HashSet<string>();

        foreach (ISearchResult document in documents)
        {
            idsToDelete.Add(document.Id);
        }

        if (idsToDelete.Any())
        {
            index.DeleteFromIndex(idsToDelete);
        }
    }

    public Task DeleteAsync(string indexAlias, IEnumerable<Guid> keys)
    {
        IIndex index = GetIndex(indexAlias);
        var idsToDelete = new HashSet<string>();

        foreach (Guid key in keys)
        {
            ISearchResults documents = index.Searcher.CreateQuery().Field(FieldNameHelper.FieldName(CoreConstants.FieldNames.PathIds, Constants.FieldValues.Keywords), key.AsKeyword()).Execute();
            foreach (ISearchResult document in documents)
            {
                idsToDelete.Add(document.Id);
            }

            index.DeleteFromIndex(idsToDelete);
        }

        return Task.CompletedTask;
    }

    private Dictionary<string, IEnumerable<object>> MapToDictionary(IEnumerable<IndexField> fields, string? culture, string? segment, ContentProtection? protection)
    {
        var result = new Dictionary<string, IEnumerable<object>>();
        List<string> aggregatedTexts = [];
        List<string> aggregatedR1Texts = [];
        List<string> aggregatedR2Texts = [];
        List<string> aggregatedR3Texts = [];
        foreach (IndexField field in fields)
        {
            if (field.Value.Integers?.Any() ?? false)
            {
                var integers = field.Value.Integers.Cast<object>().ToList();
                result.Add(FieldNameHelper.FieldName(field, Constants.FieldValues.Integers),  integers);
            }

            if (field.Value.Keywords?.Any() ?? false)
            {
                // add field for keyword filtering (will be indexed as RAW)
                var fieldName = FieldNameHelper.FieldName(field, Constants.FieldValues.Keywords);
                result.Add(fieldName, field.Value.Keywords);
                FieldOptions.Field? fieldConfiguration = _fieldOptions.Fields.FirstOrDefault(f
                    => f.PropertyName == field.FieldName && f.FieldValues == FieldValues.Keywords);
                if (fieldConfiguration?.Sortable is true || fieldConfiguration?.Facetable is true)
                {
                    // add extra field for sorting and/or faceting (will be indexed according to configuration)
                    result.Add(FieldNameHelper.QueryableKeywordFieldName(fieldName), field.Value.Keywords);
                }
            }

            if (field.Value.Decimals?.Any() ?? false)
            {
                var decimals = field.Value.Decimals.Cast<object>().ToList();
                result.Add(FieldNameHelper.FieldName(field, Constants.FieldValues.Decimals), decimals);
            }

            if (field.Value.DateTimeOffsets?.Any() ?? false)
            {
                // We have to use DateTime here, as examine facets does not play nice with DatetimeOffsets for now.
                var dates = field.Value.DateTimeOffsets.Select(x => x.DateTime).Cast<object>().ToList();
                result.Add(FieldNameHelper.FieldName(field, Constants.FieldValues.DateTimeOffsets), dates);
            }

            if (field.Value.Texts?.Any() ?? false)
            {
                result.Add(FieldNameHelper.FieldName(field, Constants.FieldValues.Texts), field.Value.Texts);
                aggregatedTexts.AddRange(field.Value.Texts);
            }

            if (field.Value.TextsR1?.Any() ?? false)
            {
                result.Add(FieldNameHelper.FieldName(field, Constants.FieldValues.TextsR1), field.Value.TextsR1);
                aggregatedR1Texts.AddRange(field.Value.TextsR1);
            }

            if (field.Value.TextsR2?.Any() ?? false)
            {
                result.Add(FieldNameHelper.FieldName(field, Constants.FieldValues.TextsR2), field.Value.TextsR2);
                aggregatedR2Texts.AddRange(field.Value.TextsR2);
            }

            if (field.Value.TextsR3?.Any() ?? false)
            {
                result.Add(FieldNameHelper.FieldName(field, Constants.FieldValues.TextsR3), field.Value.TextsR3);
                aggregatedR3Texts.AddRange(field.Value.TextsR3);
            }
        }

        if (aggregatedTexts.Any())
        {
            result.Add(Constants.SystemFields.AggregatedTexts, aggregatedTexts.ToArray());
        }

        if (aggregatedR1Texts.Any())
        {
            result.Add(Constants.SystemFields.AggregatedTextsR1, aggregatedR1Texts.ToArray());
        }

        if (aggregatedR2Texts.Any())
        {
            result.Add(Constants.SystemFields.AggregatedTextsR2, aggregatedR2Texts.ToArray());
        }

        if (aggregatedR3Texts.Any())
        {
            result.Add(Constants.SystemFields.AggregatedTextsR3, aggregatedR3Texts.ToArray());
        }

        // Cannot add null values, so we have to just say "none" here, so we can filter on variant / invariant content
        result.Add(Constants.SystemFields.Culture, [culture ?? Constants.Variance.Invariant]);
        result.Add(Constants.SystemFields.Segment, [segment ?? Constants.Variance.Invariant]);
        IEnumerable<Guid> protectionIds = protection?.AccessIds ?? new List<Guid> {Guid.Empty};
        result.Add(Constants.SystemFields.Protection, protectionIds.Select(x => x.AsKeyword()));

        return result;
    }

    private IIndex GetIndex(string indexName)
        => _examineManager.TryGetIndex(indexName, out IIndex? index)
            ? index
            : throw new ArgumentException($"The index {indexName} could not be found.", nameof(indexName));
}
