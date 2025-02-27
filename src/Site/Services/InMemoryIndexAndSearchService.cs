using Package;
using Package.Models.Indexing;
using Package.Models.Searching;
using Package.Services;

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
                    v.Value.Fields.Any(f => f.Alias == IndexConstants.Aliases.AncestorIds && f.Value.Keywords?.Contains($"{key:D}") is true)
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
                    .Single(f => f.Alias == "My_Stamp")
                    .Value
                    .Keywords!
                    .First()
                : null
        );
    }

    public IReadOnlyDictionary<Guid, IndexField[]> Dump() => Index.ToDictionary(d => d.Key, d => d.Value.Fields).AsReadOnly();

    public Task<SearchResult> SearchAsync(string? query, IEnumerable<Filter>? filters, IEnumerable<Facet>? facets, string? culture, string? segment, int skip, int take)
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
                .Any(field =>
                    (field.Culture is null || field.Culture.InvariantEquals(culture))
                    && (field.Segment is null || field.Segment.InvariantEquals(segment))
                    && (field.Value.Texts?.Any(text => text.InvariantContains(query)) ?? false)
                )
            );
        }

        // filters needs splitting into two parts; regular filters and facet filters
        // - regular filters must be applied before any facets are calculated (they narrow down the potential result set)
        // - facet filters must be applied after facets calculation has begun (additional considerations apply, see comments below)
        var facetKeys = facets?.Select(facet => facet.Key).ToArray();
        var facetFilters = filters?.Where(f => facetKeys?.InvariantContains(f.Key) is true).ToArray();
        var regularFilters = filters?.Where(f => facetKeys?.InvariantContains(f.Key) is not true).ToArray();

        if (regularFilters is not null)
        {
            result = FilterDocuments(result, regularFilters, culture, segment);
        }

        // facets needs splitting into two parts; active facets and passive facets
        // - active facets are facets that have active filters - they need calculating before applying the facet filters
        // - passive facets do not have active filters - they need calculating after applying the facet filters 
        var activeFacets = facets?.Where(facet => facetFilters?.Any(filter => filter.Key.InvariantEquals(facet.Key)) is true).ToArray();
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
                    field.Alias.InvariantEquals(filter.Key)
                    && (field.Culture is null || field.Culture.InvariantEquals(culture))
                    && (field.Segment is null || field.Segment.InvariantEquals(segment))
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
                    field.Alias.InvariantEquals(facet.Key)
                    && (field.Culture is null || field.Culture.InvariantEquals(culture))
                    && (field.Segment is null || field.Segment.InvariantEquals(segment))
                ))
                .WhereNotNull();

            var facetValues = GetFacetValues(facet,  facetFields.Select(f => f.Value));

            return new FacetResult(facet.Key, facetValues);

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
    
    private record IndexDocument(Variation[] Variations, IndexField[] Fields)
    {
    }
}
