using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Models.Searching;
using Umbraco.Cms.Search.Core.Models.Searching.Faceting;
using Umbraco.Cms.Search.Core.Models.Searching.Filtering;
using Umbraco.Cms.Search.Core.Models.Searching.Sorting;
using Umbraco.Cms.Search.Core.Services;
using Umbraco.Test.Search.Integration.Tests;
using Constants = Umbraco.Cms.Search.Core.Constants;

namespace Umbraco.Test.Search.Integration.Services;

public class TestIndexer : IIndexer, ISearcher
{
    private readonly Dictionary<string, Dictionary<Guid, TestIndexDocument>> _indexes = new();
        
    public Task AddOrUpdateAsync(string indexAlias, Guid id, UmbracoObjectTypes objectType, IEnumerable<Variation> variations, IEnumerable<IndexField> fields, ContentProtection? protection)
    {
        GetIndex(indexAlias)[id] = new (id, objectType, variations, fields, protection);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string indexAlias, IEnumerable<Guid> ids)
    {
        // index is responsible for deleting descendants
        foreach (var key in ids)
        {
            GetIndex(indexAlias).Remove(key);
            var descendantDocuments = GetIndex(indexAlias).Values.Where(document =>
                document.Fields.Any(f => f.FieldName == Constants.FieldNames.PathIds && f.Value.Keywords?.Contains($"{key:D}") is true)
            );
            foreach (var descendantDocument in descendantDocuments)
            {
                GetIndex(indexAlias).Remove(descendantDocument.Id);
            }
        }

        return Task.CompletedTask;
    }

    public IReadOnlyList<TestIndexDocument> Dump(string indexAlias) => GetIndex(indexAlias).Values.ToList();

    public void Reset() => _indexes.Clear();
    
    private Dictionary<Guid, TestIndexDocument> GetIndex(string index)
    {
        if (_indexes.ContainsKey(index) is false)
        {
            _indexes[index] = new();
        }

        return _indexes[index];
    }

    // very simplistic implementation of ISearcher to satisfy the back-office search tests
    public Task<SearchResult> SearchAsync(
        string indexAlias,
        string? query,
        IEnumerable<Filter>? filters,
        IEnumerable<Facet>? facets,
        IEnumerable<Sorter>? sorters,
        string? culture,
        string? segment,
        AccessContext? accessContext,
        int skip,
        int take)
    {
        indexAlias = indexAlias switch
        {
            Constants.IndexAliases.DraftContent => TestBase.IndexAliases.DraftContent,
            Constants.IndexAliases.DraftMedia => TestBase.IndexAliases.Media,
            _ => throw new ArgumentOutOfRangeException(nameof(indexAlias))
        };

        bool IsVarianceMatch(IndexField field)
            => (field.Culture is null || field.Culture == culture)
               && (field.Segment is null || field.Segment == culture); 
        
        var index = GetIndex(indexAlias);
        var result = index.Values.ToArray();
        if (query is not null)
        {
            result = result.Where(document =>
                document.Fields.Any(field =>
                    IsVarianceMatch(field)
                    && field.Value.Texts?.Any(text => text.Contains(query)) is true
                )
            ).ToArray();
        }

        foreach (var filter in filters ?? [])
        {
            switch (filter)
            {
                case KeywordFilter keywordFilter:
                    result = result.Where(document =>
                        document.Fields.Any(field =>
                            IsVarianceMatch(field)
                            && field.FieldName == keywordFilter.FieldName
                            && field.Value.Keywords?.ContainsAny(keywordFilter.Values) != keywordFilter.Negate
                        )
                    ).ToArray();
                    break;
                case IntegerExactFilter integerExactFilter:
                    result = result.Where(document =>
                        document.Fields.Any(field =>
                            IsVarianceMatch(field)
                            && field.FieldName == integerExactFilter.FieldName
                            && field.Value.Integers?.ContainsAny(integerExactFilter.Values) != integerExactFilter.Negate
                        )
                    ).ToArray();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(filter), "Unsupported filter type");
            }
        }

        if (sorters is not null)
        {
            var sortersAsArray = sorters as Sorter[] ?? sorters.ToArray();
            if (sortersAsArray.Length != 1)
            {
                throw new ArgumentOutOfRangeException(nameof(sorters), "Only one sorter is supported (or none at all)");
            }

            result = sortersAsArray.First() switch
            {
                StringSorter stringSorter => result.OrderBy(document =>
                    document.Fields.FirstOrDefault(field =>
                        IsVarianceMatch(field)
                        && field.FieldName == stringSorter.FieldName
                    )?.Value.Texts?.FirstOrDefault()
                ).ToArray(),
                DateTimeOffsetSorter dateTimeOffsetSorter => result.OrderBy(document =>
                    document.Fields.FirstOrDefault(field =>
                        IsVarianceMatch(field)
                        && field.FieldName == dateTimeOffsetSorter.FieldName
                    )?.Value.DateTimeOffsets?.FirstOrDefault()
                ).ToArray(),
                _ => result
            };

            if (sortersAsArray.First().Direction == Direction.Descending)
            {
                result = result.Reverse().ToArray();
            }
        }

        return Task.FromResult(
            new SearchResult(
                result.Length,
                result
                    .Skip(skip)
                    .Take(take)
                    .Select(document => new Document(document.Id, document.ObjectType))
                    .ToArray(),
                []
            )
        );
    }
}