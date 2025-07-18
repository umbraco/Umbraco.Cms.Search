using Umbraco.Cms.Core;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Models.Searching;
using Umbraco.Cms.Search.Core.Models.Searching.Faceting;
using Umbraco.Cms.Search.Core.Models.Searching.Filtering;
using Umbraco.Cms.Search.Core.Models.Searching.Sorting;
using Umbraco.Cms.Search.Provider.InMemory.Models;
using Umbraco.Extensions;

namespace Umbraco.Cms.Search.Provider.InMemory.Services;

internal sealed class InMemorySearcher : IInMemorySearcher
{
    private readonly DataStore _dataStore;

    public InMemorySearcher(DataStore dataStore)
        => _dataStore = dataStore;

    public Task<SearchResult> SearchAsync(
        string indexAlias,
        string? query = null,
        IEnumerable<Filter>? filters = null,
        IEnumerable<Facet>? facets = null,
        IEnumerable<Sorter>? sorters = null,
        string? culture = null,
        string? segment = null,
        AccessContext? accessContext = null,
        int skip = 0,
        int take = 10)
    {
        if (_dataStore.TryGetValue(indexAlias, out var index) is false)
        {
            throw new ArgumentException($"The index \"{indexAlias}\" was not defined.", nameof(indexAlias));
        }
        
        var result = index.Where(kvp => kvp
            .Value
            .Variations
            .Any(variation =>
                (variation.Culture is null || variation.Culture.InvariantEquals(culture))
                && (variation.Segment is null || variation.Segment.InvariantEquals(segment))
            )
        );

        var accessKeys = accessContext?.PrincipalId.Yield().Union(accessContext.GroupIds ?? []).ToArray();
        result = result.Where(kvp =>
            kvp.Value.Protection is null
            || (accessKeys is not null && kvp.Value.Protection.AccessIds.ContainsAny(accessKeys)
            )
        );
        
        if (query.IsNullOrWhiteSpace() is false)
        {
            result = result.Where(kvp => kvp
                .Value
                .Fields
                .Any(field => FieldMatcher.IsMatch(field, kvp.Value, null, culture, segment)
                              && AllTexts(field.Value).Any(text => text.InvariantContains(query)))
            );
        }

        // filters needs splitting into two parts; regular filters (not used for faceting) and facet filters
        // - regular filters must be applied before any facets are calculated (they narrow down the potential result set)
        // - facet filters must be applied after facets calculation has begun (additional considerations apply, see comments below)
        var facetFieldNames = facets?.Select(facet => facet.FieldName).ToArray();
        var facetFilters = filters?.Where(f => facetFieldNames?.InvariantContains(f.FieldName) is true).ToArray();
        var regularFilters = filters?.Except(facetFilters ?? []).ToArray();

        if (regularFilters is not null)
        {
            result = FilterDocuments(result, regularFilters, culture, segment);
        }

        // facets needs splitting into two parts; active facets and passive facets
        // - active facets are facets that have active filters - they need calculating before applying the facet filters
        // - passive facets do not have active filters - they need calculating after applying the facet filters 
        var activeFacets = facets?.Where(facet => facetFilters?.Any(filter => filter.FieldName.InvariantEquals(facet.FieldName)) is true).ToArray();
        var passiveFacets = facets?.Except(activeFacets ?? []).ToArray();
        
        var facetResults = new List<FacetResult>();
        if (activeFacets is not null)
        {
            result = result.ToArray();
            facetResults.AddRange(ExtractFacets(result, activeFacets, culture, segment));
        }

        if (facetFilters is not null)
        {
            result = FilterDocuments(result, facetFilters, culture, segment);
        }

        if (passiveFacets is not null)
        {
            result = result.ToArray();
            facetResults.AddRange(ExtractFacets(result, passiveFacets, culture, segment));
        }

        // default sorting = by score, descending
        sorters ??= [new ScoreSorter(Direction.Descending)];
        result = SortDocuments(result, sorters.ToArray(), culture, segment);
        
        var resultAsArray = result.ToArray();
        return Task.FromResult(
            new SearchResult(
                resultAsArray.Length,
                resultAsArray.Skip(skip).Take(take).Select(kpv => new Document(kpv.Key, kpv.Value.ObjectType)).ToArray(),
                facetResults
            )
        );
    }
    
    private IEnumerable<KeyValuePair<Guid, IndexDocument>> FilterDocuments(IEnumerable<KeyValuePair<Guid, IndexDocument>> documents, Filter[] filters, string? culture, string? segment)
    {
        foreach (var filter in filters)
        {
            documents = documents.Where(kvp =>
                kvp.Value.Fields.Any(field =>
                    FieldMatcher.IsMatch(field, kvp.Value, filter.FieldName, culture, segment)
                    && IsFilterMatch(filter, field.Value)
                )
            );
        }
            
        return documents;

        bool IsFilterMatch(Filter filter, IndexValue value)
        {
            var isMatch = filter switch
            {
                TextFilter textFilter => AllTexts(value).Any(t => textFilter.Values.Any(t.InvariantContains)),
                KeywordFilter keywordFilter => value.Keywords?.ContainsAny(keywordFilter.Values) ?? false,
                IntegerExactFilter integerExactFilter => value.Integers?.ContainsAny(integerExactFilter.Values) ?? false,
                IntegerRangeFilter integerRangeFilter => value.Integers?.Any(i => integerRangeFilter.Ranges.Any(r => i >= (r.Min ?? int.MinValue) && i <= (r.Max ?? int.MaxValue))) ?? false,
                DecimalExactFilter decimalExactFilter => value.Decimals?.ContainsAny(decimalExactFilter.Values) ?? false,
                DecimalRangeFilter decimalRangeFilter => value.Decimals?.Any(i => decimalRangeFilter.Ranges.Any(r => i >= (r.Min ?? decimal.MinValue) && i <= (r.Max ?? decimal.MaxValue))) ?? false,
                DateTimeOffsetExactFilter dateTimeOffsetExactFilter => value.DateTimeOffsets?.ContainsAny(dateTimeOffsetExactFilter.Values) ?? false,
                DateTimeOffsetRangeFilter dateTimeOffsetRangeFilter => value.DateTimeOffsets?.Any(i => dateTimeOffsetRangeFilter.Ranges.Any(r => i >= (r.Min ?? DateTimeOffset.MinValue) && i <= (r.Max ?? DateTimeOffset.MaxValue))) ?? false,
                _ => throw new ArgumentOutOfRangeException(nameof(filter), $"Encountered an unsupported filter type: {filter.GetType().Name}")
            };

            return isMatch != filter.Negate;
        }
    }

    private IEnumerable<FacetResult> ExtractFacets(IEnumerable<KeyValuePair<Guid, IndexDocument>> documents, Facet[] facets, string? culture, string? segment)
        => facets.Select(facet =>
        {
            var facetFields = documents
                .Select(candidate => candidate.Value.Fields.FirstOrDefault(field =>
                    FieldMatcher.IsMatch(field, candidate.Value, facet.FieldName, culture, segment)
                ))
                .WhereNotNull();

            var facetValues = GetFacetValues(facet,  facetFields.Select(f => f.Value));

            return new FacetResult(facet.FieldName, facetValues);

            IEnumerable<FacetValue> GetFacetValues(Facet facet, IEnumerable<IndexValue> values)
                => facet switch
                {
                    KeywordFacet => values.SelectMany(v => v.Keywords ?? []).GroupBy(v => v).Select(g => new KeywordFacetValue(g.Key, g.Count())),
                    IntegerExactFacet => values.SelectMany(v => v.Integers ?? []).GroupBy(v => v).Select(g => new IntegerExactFacetValue(g.Key, g.Count())),
                    DecimalExactFacet => values.SelectMany(v => v.Decimals ?? []).GroupBy(v => v).Select(g => new DecimalExactFacetValue(g.Key, g.Count())),
                    DateTimeOffsetExactFacet => values.SelectMany(v => v.DateTimeOffsets ?? []).GroupBy(v => v).Select(g => new DateTimeOffsetExactFacetValue(g.Key, g.Count())),
                    IntegerRangeFacet integerRangeFacet => ExtractIntegerRangeFacetValues(integerRangeFacet, values), 
                    DecimalRangeFacet decimalRangeFacet => ExtractDecimalRangeFacetValues(decimalRangeFacet, values),
                    DateTimeOffsetRangeFacet dateTimeOffsetRangeFacet => ExtractDateTimeOffsetRangeFacetValues(dateTimeOffsetRangeFacet, values),
                    _ => throw new ArgumentOutOfRangeException(nameof(facet), $"Encountered an unsupported facet type: {facet.GetType().Name}")
                }; 
        }).ToArray();

    private IntegerRangeFacetValue[] ExtractIntegerRangeFacetValues(IntegerRangeFacet facet, IEnumerable<IndexValue> values)
    {
        var allValues = values.SelectMany(v => v.Integers ?? []).ToArray();
        return facet
            .Ranges
            .Select(range => new IntegerRangeFacetValue(
                range.Key,
                range.Min,
                range.Max,
                allValues.Count(v => v > (range.Min ?? int.MinValue) && v <= (range.Max ?? int.MaxValue)))
            ).ToArray();
    }
    
    private DecimalRangeFacetValue[] ExtractDecimalRangeFacetValues(DecimalRangeFacet facet, IEnumerable<IndexValue> values)
    {
        var allValues = values.SelectMany(v => v.Decimals ?? []).ToArray();
        return facet
            .Ranges
            .Select(range => new DecimalRangeFacetValue(
                range.Key,
                range.Min,
                range.Max,
                allValues.Count(v => v > (range.Min ?? decimal.MinValue) && v <= (range.Max ?? decimal.MaxValue)))
            ).ToArray();
    }

    private DateTimeOffsetRangeFacetValue[] ExtractDateTimeOffsetRangeFacetValues(DateTimeOffsetRangeFacet facet, IEnumerable<IndexValue> values)
    {
        var allValues = values.SelectMany(v => v.DateTimeOffsets ?? []).ToArray();
        return facet
            .Ranges
            .Select(range => new DateTimeOffsetRangeFacetValue(
                range.Key,
                range.Min,
                range.Max,
                allValues.Count(v => v > (range.Min ?? DateTimeOffset.MinValue) && v <= (range.Max ?? DateTimeOffset.MaxValue)))
            ).ToArray();
    }

    private IEnumerable<KeyValuePair<Guid, IndexDocument>> SortDocuments(IEnumerable<KeyValuePair<Guid, IndexDocument>> documents, Sorter[] sorters, string? culture, string? segment)
    {
        var sorter = sorters.FirstOrDefault() ?? throw new ArgumentException("Expected one or more sorters.", nameof(sorters));

        if (sorter is ScoreSorter)
        {
            return documents.OrderBy(d => d.Key, sorter.Direction);
        }

        var comparer = new SorterComparer(sorter, culture, segment);
        return sorter.Direction is Direction.Ascending
            ? documents.OrderBy(d => d.Value, comparer)
            : documents.OrderByDescending(d => d.Value, comparer);
    }

    private static IEnumerable<string> AllTexts(IndexValue indexValue)
        => indexValue.TextsR1.EmptyNull()
            .Union(indexValue.TextsR2.EmptyNull())
            .Union(indexValue.TextsR3.EmptyNull())
            .Union(indexValue.Texts.EmptyNull());

    private class SorterComparer : IComparer<IndexDocument>
    {
        private readonly Sorter _sorter;
        private readonly string? _culture;
        private readonly string? _segment;

        public SorterComparer(Sorter sorter, string? culture, string? segment)
        {
            _sorter = sorter;
            _culture = culture;
            _segment = segment;
        }

        public int Compare(IndexDocument? x, IndexDocument? y)
        {
            var xField = x?.Fields.FirstOrDefault(field => FieldMatcher.IsMatch(field, x, _sorter.FieldName, _culture, _segment));
            var yField = y?.Fields.FirstOrDefault(field => FieldMatcher.IsMatch(field, y, _sorter.FieldName, _culture, _segment));

            var xFieldValue = FieldValue(xField);
            var yFieldValue = FieldValue(yField);

            return xFieldValue is not null
                ? xFieldValue.CompareTo(yFieldValue)
                : yFieldValue is not null
                    ? -1 * yFieldValue.CompareTo(xFieldValue)
                    : 0;
        }

        private IComparable? FieldValue(IndexField? field)
            => field is null
                ? null
                : _sorter switch
                {
                    DateTimeOffsetSorter => field.Value.DateTimeOffsets?.FirstOrDefault(),
                    DecimalSorter => field.Value.Decimals?.FirstOrDefault(),
                    IntegerSorter => field.Value.Integers?.FirstOrDefault(),
                    KeywordSorter => field.Value.Keywords?.FirstOrDefault(),
                    TextSorter => AllTexts(field.Value).FirstOrDefault(),
                    _ => throw new ArgumentOutOfRangeException($"Unsupported sorter type: {_sorter.GetType().FullName}")
                };
    }

    private static class FieldMatcher
    {
        public static bool IsMatch(IndexField field, IndexDocument document, string? fieldName, string? culture, string? segment)
        {
            bool IsExactMatch(IndexField f)
                => IsSegmentLessMatch(f) && f.Segment.InvariantEquals(segment);

            bool IsSegmentLessMatch(IndexField f)
                => (fieldName is null || f.FieldName.InvariantEquals(fieldName))
                   && (f.Culture is null || f.Culture.InvariantEquals(culture));

            if (IsExactMatch(field))
            {
                return true;
            }
            
            return IsSegmentLessMatch(field) && document.Fields.Any(IsExactMatch) is false;
        }
    }
}