using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Extensions;

namespace Umbraco.Cms.Search.Core.Services.ContentIndexing;

public sealed class ContentIndexingDataCollectionService : IContentIndexingDataCollectionService
{
    private readonly ISet<IContentIndexer> _contentIndexers;
    private readonly ILogger<ContentIndexingDataCollectionService> _logger;

    public ContentIndexingDataCollectionService(
        IEnumerable<IContentIndexer> contentIndexers,
        ILogger<ContentIndexingDataCollectionService> logger)
    {
        _contentIndexers = contentIndexers.ToHashSet();
        _logger = logger;
    }

    public async Task<IEnumerable<IndexField>?> CollectAsync(IContent content, bool published,
        CancellationToken cancellationToken)
    {
        var systemFieldsIndexers = _contentIndexers.OfType<ISystemFieldsContentIndexer>().ToArray();
        if (systemFieldsIndexers.Length != 1)
        {
            throw new InvalidOperationException("One and only one system fields content indexer must be present.");
        }

        var cultures = published ? PublishedCultures(content) : AvailableCultures(content);
        if (cultures.Length is 0)
        {
            return null;
        }

        var systemFieldsIndexer = systemFieldsIndexers.First();
        var systemFields = await systemFieldsIndexer.GetIndexFieldsAsync(content, cultures, published, cancellationToken);
        
        string Identifier(IndexField field) => $"{field.FieldName}|{field.Culture}|{field.Segment}";
        var fieldsByIdentifier = systemFields.ToDictionary(Identifier);

        foreach (var contentIndexer in _contentIndexers.Except(systemFieldsIndexers))
        {
            var fields = await contentIndexer.GetIndexFieldsAsync(content, cultures, published, cancellationToken);
            foreach (var field in fields)
            {
                if (fieldsByIdentifier.TryAdd(Identifier(field), field) is false)
                {
                    _logger.LogWarning(
                        "Duplicate index field with alias {alias} (culture {culture}, segment {segment}) was detected and ignored - caused by indexer {indexer} while indexing content item {contentKey}",
                        field.FieldName,
                        field.Culture ?? "[null]",
                        field.Segment ?? "[null]",
                        contentIndexer.GetType().FullName,
                        content.Key);
                }
            }
        }

        return fieldsByIdentifier.Values;
    }
    
    private string?[] AvailableCultures(IContent content)
        => content.ContentType.VariesByCulture()
            ? content.AvailableCultures.ToArray()
            : [null];

    private string?[] PublishedCultures(IContent content)
        => content.ContentType.VariesByCulture()
            ? content.PublishedCultures.ToArray()
            : [null];
}
