using Examine;
using Examine.Search;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Extensions;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Models.ViewModels;
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

        IEnumerable<IGrouping<string?, Variation>> variationGroups = variations.GroupBy(x => x.Culture);

        IndexField[] fieldsAsArray = fields as IndexField[] ?? fields.ToArray();

        foreach (IGrouping<string?, Variation> variationGroup in variationGroups)
        {
            var indexKey = CalculateIndexKey(key, variationGroup.Key);
            IEnumerable<IndexField> fieldsToMap = MapFields(fieldsAsArray.Where(x => x.Culture is null || x.Culture == variationGroup.Key), variationGroup.Key);

            valuesToIndex.Add(new ValueSet(
                indexKey,
                objectType.ToString(),
                MapToDictionary(fieldsToMap, variationGroup.Key, variationGroup.Select(x => x.Segment).Distinct(), protection)));
        }

        index.IndexItems(valuesToIndex);

        return Task.CompletedTask;
    }

    private IEnumerable<IndexField> MapFields(IEnumerable<IndexField> fields, string? culture)
    {
        var results = new Dictionary<(string FieldName, string? Segment), IndexField>();
        foreach (IndexField field in fields)
        {
            (string FieldName, string? Segment) key = (field.FieldName, field.Segment);

            if (field.Culture is null)
            {
                if (results.TryGetValue(key, out IndexField? indexField))
                {
                    results[key] = field with { Value = MergeIndexValue(indexField.Value, field.Value), Culture = culture };
                    continue;
                }

                results.Add(key, field);
            }

            if (field.Culture == culture)
            {
                if (results.TryGetValue(key, out IndexField? indexField))
                {
                    results[key] = field with { Value = MergeIndexValue(indexField.Value, field.Value) };
                    continue;
                }

                results[key] = field;
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

    public Task<long> GetDocumentCountAsync(string indexAlias)
    {
        if (_examineManager.TryGetIndex(indexAlias, out IIndex? index))
        {
            if (index is IIndexStats stats)
            {
                return Task.FromResult(stats.GetDocumentCount());
            }
        }

        return Task.FromResult(0L);
    }

    public Task<HealthStatus> GetHealthStatus(string indexAlias)
    {
        if (_examineManager.TryGetIndex(indexAlias, out IIndex? index) is false || index.IndexExists() is false)
        {
            return Task.FromResult(HealthStatus.Unknown);
        }

        if (index is IIndexStats stats && stats.GetDocumentCount() == 0)
        {
            return Task.FromResult(HealthStatus.Empty);
        }

        // Attempt to query the index to verify it's readable and not corrupted
        try
        {
            index.Searcher.CreateQuery().ManagedQuery("__healthcheck__").Execute(new QueryOptions(0, 1));
            return Task.FromResult(HealthStatus.Healthy);
        }
        catch
        {
            return Task.FromResult(HealthStatus.Corrupted);
        }
    }

    private static string CalculateIndexKey(Guid key, string? culture)
    {
        var result = key.ToString().ToLowerInvariant();

        if (culture is not null)
        {
            result += $"_{culture}";
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

    private Dictionary<string, IEnumerable<object>> MapToDictionary(IEnumerable<IndexField> fields, string? culture, IEnumerable<string?> segments, ContentProtection? protection)
    {
        var result = new Dictionary<string, IEnumerable<object>>();

        // Aggregated texts grouped by segment (using empty string for null segment)
        var aggregatedTextsBySegment = new Dictionary<string, List<string>>();
        var aggregatedR1TextsBySegment = new Dictionary<string, List<string>>();
        var aggregatedR2TextsBySegment = new Dictionary<string, List<string>>();
        var aggregatedR3TextsBySegment = new Dictionary<string, List<string>>();

        foreach (IndexField field in fields)
        {
            if (field.Value.Integers?.Any() ?? false)
            {
                var integers = field.Value.Integers.Cast<object>().ToList();
                result.Add(FieldNameHelper.FieldName(field, Constants.FieldValues.Integers), integers);
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
                AddToAggregatedTexts(aggregatedTextsBySegment, field.Segment, field.Value.Texts);
            }

            if (field.Value.TextsR1?.Any() ?? false)
            {
                result.Add(FieldNameHelper.FieldName(field, Constants.FieldValues.TextsR1), field.Value.TextsR1);
                AddToAggregatedTexts(aggregatedR1TextsBySegment, field.Segment, field.Value.TextsR1);
            }

            if (field.Value.TextsR2?.Any() ?? false)
            {
                result.Add(FieldNameHelper.FieldName(field, Constants.FieldValues.TextsR2), field.Value.TextsR2);
                AddToAggregatedTexts(aggregatedR2TextsBySegment, field.Segment, field.Value.TextsR2);
            }

            if (field.Value.TextsR3?.Any() ?? false)
            {
                result.Add(FieldNameHelper.FieldName(field, Constants.FieldValues.TextsR3), field.Value.TextsR3);
                AddToAggregatedTexts(aggregatedR3TextsBySegment, field.Segment, field.Value.TextsR3);
            }
        }

        // Add segment-specific aggregated text fields
        AddAggregatedTextFields(result, Constants.SystemFields.AggregatedTexts, aggregatedTextsBySegment);
        AddAggregatedTextFields(result, Constants.SystemFields.AggregatedTextsR1, aggregatedR1TextsBySegment);
        AddAggregatedTextFields(result, Constants.SystemFields.AggregatedTextsR2, aggregatedR2TextsBySegment);
        AddAggregatedTextFields(result, Constants.SystemFields.AggregatedTextsR3, aggregatedR3TextsBySegment);

        // Cannot add null values, so we have to just say "none" here, so we can filter on variant / invariant content
        result.Add(Constants.SystemFields.Culture, [culture ?? Constants.Variance.Invariant]);
        IEnumerable<Guid> protectionIds = protection?.AccessIds ?? new List<Guid> {Guid.Empty};
        result.Add(Constants.SystemFields.Protection, protectionIds.Select(x => x.AsKeyword()));

        return result;
    }

    private static void AddToAggregatedTexts(Dictionary<string, List<string>> aggregatedTextsBySegment, string? segment, IEnumerable<string> texts)
    {
        // Use empty string as key for null segment
        var key = segment ?? string.Empty;
        if (aggregatedTextsBySegment.TryGetValue(key, out List<string>? list))
        {
            list.AddRange(texts);
        }
        else
        {
            aggregatedTextsBySegment[key] = texts.ToList();
        }
    }

    private static void AddAggregatedTextFields(Dictionary<string, IEnumerable<object>> result, string baseFieldName, Dictionary<string, List<string>> aggregatedTextsBySegment)
    {
        foreach (KeyValuePair<string, List<string>> aggregatedTexts in aggregatedTextsBySegment)
        {
            if (!aggregatedTexts.Value.Any())
            {
                continue;
            }

            // Empty string key means null segment, use base field name
            var fieldName = string.IsNullOrEmpty(aggregatedTexts.Key)
                ? baseFieldName
                : $"{baseFieldName}_{aggregatedTexts.Key}";

            result.Add(fieldName, aggregatedTexts.Value.ToArray());
        }
    }

    private IIndex GetIndex(string indexName)
        => _examineManager.TryGetIndex(indexName, out IIndex? index)
            ? index
            : throw new ArgumentException($"The index {indexName} could not be found.", nameof(indexName));
}
