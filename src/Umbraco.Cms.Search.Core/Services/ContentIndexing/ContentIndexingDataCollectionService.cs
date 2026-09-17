using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Extensions;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Models.Persistence;

namespace Umbraco.Cms.Search.Core.Services.ContentIndexing;

internal sealed class ContentIndexingDataCollectionService : IContentIndexingDataCollectionService
{
    private readonly ISet<IContentIndexer> _contentIndexers;
    private readonly ILogger<ContentIndexingDataCollectionService> _logger;
    private readonly IIndexDocumentService _indexDocumentService;

    public ContentIndexingDataCollectionService(IEnumerable<IContentIndexer> contentIndexers, ILogger<ContentIndexingDataCollectionService> logger, IIndexDocumentService indexDocumentService)
    {
        _contentIndexers = contentIndexers.ToHashSet();
        _logger = logger;
        _indexDocumentService = indexDocumentService;
    }

    public async Task<IEnumerable<IndexField>?> CollectAsync(IContentBase content, bool published, CancellationToken cancellationToken)
    {
        IndexDocument? document = await _indexDocumentService.GetAsync(content.Key, published);
        if (document is not null)
        {
            return document.Fields;
        }

        ISystemFieldsContentIndexer[] systemFieldsIndexers = _contentIndexers.OfType<ISystemFieldsContentIndexer>().ToArray();
        if (systemFieldsIndexers.Length != 1)
        {
            throw new InvalidOperationException("One and only one system fields content indexer must be present.");
        }

        var cultures = published ? content.PublishedCultures() : content.AvailableCultures();
        if (cultures.Length is 0)
        {
            return null;
        }

        ISystemFieldsContentIndexer systemFieldsIndexer = systemFieldsIndexers.First();
        IEnumerable<IndexField> systemFields = await systemFieldsIndexer.GetIndexFieldsAsync(content, cultures, published, cancellationToken);

        string Identifier(IndexField field) => $"{field.FieldName}|{field.Culture}|{field.Segment}";
        var fieldsByIdentifier = systemFields.ToDictionary(Identifier);

        foreach (IContentIndexer contentIndexer in _contentIndexers.Except(systemFieldsIndexers))
        {
            IEnumerable<IndexField> fields = await contentIndexer.GetIndexFieldsAsync(content, cultures, published, cancellationToken);
            foreach (IndexField field in fields)
            {
                var identifier = Identifier(field);
                if (fieldsByIdentifier.TryGetValue(identifier, out IndexField? existingField))
                {
                    // If multiple contex indexers have the same field, merge the values.
                    _logger.LogDebug(
                        "Index field with alias {alias} (culture {culture}, segment {segment}) was contributed by more than one indexer - merging values from {indexer} while indexing content item {contentKey}",
                        field.FieldName,
                        field.Culture ?? "[null]",
                        field.Segment ?? "[null]",
                        contentIndexer.GetType().FullName,
                        content.Key);
                    fieldsByIdentifier[identifier] = existingField with { Value = existingField.Value.Merge(field.Value) };
                }
                else
                {
                    fieldsByIdentifier.Add(identifier, field);
                }
            }
        }

        IndexField[] fieldsArray = fieldsByIdentifier.Values.ToArray();

        await _indexDocumentService.AddAsync(new IndexDocument()
        {
            Key = content.Key,
            Fields = fieldsArray,
            Published = published,
        });

        return fieldsArray;
    }
}
