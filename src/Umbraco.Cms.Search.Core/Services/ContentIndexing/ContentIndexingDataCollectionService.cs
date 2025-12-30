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
    private readonly IDocumentService _documentService;

    public ContentIndexingDataCollectionService(IEnumerable<IContentIndexer> contentIndexers, ILogger<ContentIndexingDataCollectionService> logger, IDocumentService documentService)
    {
        _contentIndexers = contentIndexers.ToHashSet();
        _logger = logger;
        _documentService = documentService;
    }

    public async Task<IEnumerable<IndexField>?> CollectAsync(IContentBase content, bool published, CancellationToken cancellationToken)
    {
        Document? document = await _documentService.GetAsync(content.Key, published);
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

        IndexField[] fieldsArray = fieldsByIdentifier.Values.ToArray();

        await _documentService.AddAsync(new Document()
        {
            DocumentKey =  content.Key,
            Fields = fieldsArray,
            Published = published,
        });
        return fieldsArray;
    }
}
