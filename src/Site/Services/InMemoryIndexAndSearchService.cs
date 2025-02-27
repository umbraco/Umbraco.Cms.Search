using Package;
using Package.Models.Indexing;
using Package.Models.Searching;
using Package.Models.Searching.Faceting;
using Package.Models.Searching.Filtering;
using Package.Models.Searching.Sorting;
using Package.Services;
using Umbraco.Cms.Core;

namespace Site.Services;

internal sealed class InMemoryIndexAndSearchService : IIndexService, ISearchService
{
    // static because index data is shared between indexer and searcher (individually registered in the DI) 
    private static readonly Dictionary<Guid, IndexDocument> Index = new();
    
    public Task AddOrUpdateAsync(Guid key, string stamp, IEnumerable<Variation> variations, IEnumerable<IndexField> fields)
    {
        Remove(key);
        Index[key] = new IndexDocument(
            variations.ToArray(),
            fields
                .Union([new IndexField("My_Stamp", new IndexValue { Keywords = [stamp] }, null, null)])
                .ToArray()
        );
        return Task.CompletedTask;
    }

    public Task DeleteAsync(IEnumerable<Guid> keys)
    {
        var keysArray = keys as Guid[] ?? keys.ToArray();

        // index is responsible for deleting descendants!
        foreach (var key in keysArray)
        {
            Remove(key);
            var descendantKeys = Index.Where(v =>
                    v.Value.Fields.Any(f => f.FieldName == IndexConstants.FieldNames.AncestorIds && f.Value.Keywords?.Contains($"{key:D}") is true)
                )
                .Select(pair => pair.Key);
            foreach (var descendantKey in descendantKeys)
            {
                Remove(descendantKey);
            }
        }

        return Task.CompletedTask;
    }

    private void Remove(Guid key)
    {
        Index.Remove(key);
    }
    
    public Task<string?> GetStampAsync(Guid key)
    {
        return Task.FromResult(
            Index.TryGetValue(key, out var document)
                ? document
                    .Fields
                    .Single(f => f.FieldName == "My_Stamp")
                    .Value
                    .Keywords!
                    .First()
                : null
        );
    }

    public IReadOnlyDictionary<Guid, IndexField[]> Dump() => Index.ToDictionary(d => d.Key, d => d.Value.Fields).AsReadOnly();

    public Task<SearchResult> SearchAsync(string? query, IEnumerable<Filter>? filters, IEnumerable<Facet>? facets, IEnumerable<Sorter>? sorters, string? culture, string? segment, int skip, int take)
    {
        var result = Index.Where(kvp => kvp
            .Value
            .Variations
            .Any(variation =>
                (variation.Culture is null || variation.Culture.InvariantEquals(culture))
                && (variation.Segment is null || variation.Segment.InvariantEquals(segment))
            )
        );

        if (query.IsNullOrWhiteSpace() is false)
        {
            result = result.Where(kvp => kvp
                .Value
                .Fields
                .Any(field => FieldMatcher.IsMatch(field, null, culture, segment)
                              && (field.Value.Texts?.Any(text => text.InvariantContains(query)) ?? false)
                )
            );
        }

        // filters needs splitting into two parts; regular filters and facet filters
        // - regular filters must be applied before any facets are calculated (they narrow down the potential result set)
        // - facet filters must be applied after facets calculation has begun (additional considerations apply, see comments below)
        var facetFieldNames = facets?.Select(facet => facet.FieldName).ToArray();
        var facetFilters = filters?.Where(f => facetFieldNames?.InvariantContains(f.FieldName) is true).ToArray();
        var regularFilters = filters?.Where(f => facetFieldNames?.InvariantContains(f.FieldName) is not true).ToArray();

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
                resultAsArray.Skip(skip).Take(take).Select(kpv => kpv.Key).ToArray(),
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
                    FieldMatcher.IsMatch(field, filter.FieldName, culture, segment)
                    && IsFilterMatch(filter, field.Value)
                )
            );
        }
            
        return documents;

        bool IsFilterMatch(Filter filter, IndexValue value)
        {
            var isMatch = filter switch
            {
                StringContainsFilter stringContainsFilter => value.Texts?.Any(t => stringContainsFilter.Values.Any(t.InvariantContains)) ?? false,
                StringExactFilter stringExactFilter => value.Keywords?.ContainsAny(stringExactFilter.Values) ?? false,
                IntegerExactFilter integerExactFilter => value.Integers?.ContainsAny(integerExactFilter.Values) ?? false,
                IntegerRangeFilter integerRangeFilter => value.Integers?.Any(i => i >= (integerRangeFilter.MinimumValue ?? int.MinValue) && i <= (integerRangeFilter.MaximumValue ?? int.MaxValue)) ?? false,
                DecimalExactFilter decimalExactFilter => value.Decimals?.ContainsAny(decimalExactFilter.Values) ?? false,
                DecimalRangeFilter decimalRangeFilter => value.Decimals?.Any(i => i >= (decimalRangeFilter.MinimumValue ?? decimal.MinValue) && i <= (decimalRangeFilter.MaximumValue ?? decimal.MaxValue)) ?? false,
                DateTimeOffsetExactFilter dateTimeOffsetExactFilter => value.DateTimeOffsets?.ContainsAny(dateTimeOffsetExactFilter.Values) ?? false,
                DateTimeOffsetRangeFilter dateTimeOffsetRangeFilter => value.DateTimeOffsets?.Any(i => i >= (dateTimeOffsetRangeFilter.MinimumValue ?? DateTimeOffset.MinValue) && i <= (dateTimeOffsetRangeFilter.MaximumValue ?? DateTimeOffset.MaxValue)) ?? false,
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
                    FieldMatcher.IsMatch(field, facet.FieldName, culture, segment)
                ))
                .WhereNotNull();

            var facetValues = GetFacetValues(facet,  facetFields.Select(f => f.Value));

            return new FacetResult(facet.FieldName, facetValues);

            // TODO: implement range facets
            // TODO: implement decimal and date facets
            IEnumerable<FacetValue> GetFacetValues(Facet facet, IEnumerable<IndexValue> values)
                => facet switch
                {
                    StringExactFacet => values.SelectMany(v => v.Keywords ?? []).GroupBy(v => v).Select(g => new StringExactFacetValue(g.Key, g.Count())),
                    IntegerExactFacet => values.SelectMany(v => v.Integers ?? []).GroupBy(v => v).Select(g => new IntegerExactFacetValue(g.Key, g.Count())),
                    _ => throw new ArgumentOutOfRangeException(nameof(facet), $"Encountered an unsupported facet type: {facet.GetType().Name}")
                }; 
        }).ToArray();
        
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
            var xField = x?.Fields.FirstOrDefault(field => FieldMatcher.IsMatch(field, _sorter.FieldName, _culture, _segment));
            var yField = y?.Fields.FirstOrDefault(field => FieldMatcher.IsMatch(field, _sorter.FieldName, _culture, _segment));

            var xFieldValue = FieldValue(xField);
            var yFieldValue = FieldValue(yField);

            return xFieldValue is not null
                ? xFieldValue.CompareTo(yFieldValue)
                : yFieldValue is not null
                    ? 1
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
                    StringSorter => field.Value.Texts?.FirstOrDefault(),
                    _ => throw new ArgumentOutOfRangeException($"Unsupported sorter type: {_sorter.GetType().FullName}")
                };
    }
    
    private record IndexDocument(Variation[] Variations, IndexField[] Fields)
    {
    }

    private static class FieldMatcher
    {
        public static bool IsMatch(IndexField field, string? fieldName, string? culture, string? segment)
        {
            return (fieldName is null || field.FieldName.InvariantEquals(fieldName))
                   && (field.Culture is null || field.Culture.InvariantEquals(culture))
                   && (field.Segment is null || field.Segment.InvariantEquals(segment));
        }
    }
}
