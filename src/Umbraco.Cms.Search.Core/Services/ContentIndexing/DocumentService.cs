using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Search.Core.Extensions;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Models.Persistence;
using Umbraco.Cms.Search.Core.Persistence;
using Umbraco.Extensions;

namespace Umbraco.Cms.Search.Core.Services.ContentIndexing;

public class DocumentService : IDocumentService
{
    private readonly ICoreScopeProvider _scopeProvider;
    private readonly IDocumentRepository _documentRepository;
    private readonly IContentIndexingDataCollectionService _contentIndexingDataCollectionService;

    public DocumentService(ICoreScopeProvider scopeProvider, IDocumentRepository documentRepository, IContentIndexingDataCollectionService contentIndexingDataCollectionService)
    {
        _scopeProvider = scopeProvider;
        _documentRepository = documentRepository;
        _contentIndexingDataCollectionService = contentIndexingDataCollectionService;
    }

    public async Task<Document?> GetAsync(IContentBase content, string indexAlias, bool published, CancellationToken cancellationToken)
    {
        Document? document = await CalculateDocumentAsync(content, indexAlias, published, cancellationToken);

        return document;
    }

    public async Task<IReadOnlyDictionary<Guid, Document>> GetManyAsync(IEnumerable<IContentBase> contents, string indexAlias, bool published, CancellationToken cancellationToken)
    {
        IContentBase[] contentsArray = contents as IContentBase[] ?? contents.ToArray();

        var result = new Dictionary<Guid, Document>();
        foreach (IContentBase content in contentsArray)
        {
            Document? document = await CalculateDocumentAsync(content, indexAlias, published, cancellationToken);
            if (document is not null)
            {
                result[content.Key] = document;
            }
        }

        return result;
    }

    public async Task AddAsync(Document document)
    {
        using ICoreScope scope = _scopeProvider.CreateCoreScope();
        await _documentRepository.AddAsync(document);
        scope.Complete();
    }

    public async Task DeleteAsync(Guid id, string indexAlias)
    {
        using ICoreScope scope = _scopeProvider.CreateCoreScope();
        await _documentRepository.DeleteAsync(id, indexAlias);
        scope.Complete();
    }

    public async Task<IEnumerable<Document>> GetByIndexAliasAsync(string indexAlias)
    {
        using ICoreScope scope = _scopeProvider.CreateCoreScope();
        IEnumerable<Document> documents = await _documentRepository.GetByIndexAliasAsync(indexAlias);
        scope.Complete();
        return documents;
    }

    private async Task<Document?> CalculateDocumentAsync(IContentBase content, string indexAlias, bool published, CancellationToken cancellationToken)
    {
        // Not in database, calculate fields and persist
        IEnumerable<IndexField>? fields = await _contentIndexingDataCollectionService.CollectAsync(content, published, cancellationToken);
        if (fields is null)
        {
            return null;
        }

        return new Document
        {
            DocumentKey = content.Key,
            Index = indexAlias,
            Fields = fields.ToArray(),
            ObjectType = UmbracoObjectTypes.Document,
            Variations = GetVariations(content)
        };
    }

    private Variation[] GetVariations(IContentBase content)
    {
        string?[] cultures = content.AvailableCultures();

        return content.ContentType.VariesBySegment()
            ? cultures
                .SelectMany(culture => content
                    .Properties
                    .SelectMany(property => property.Values.Where(value => value.Culture.InvariantEquals(culture)))
                    .DistinctBy(value => value.Segment).Select(value => value.Segment)
                    .Select(segment => new Variation(culture, segment)))
                .ToArray()
            : cultures
                .Select(culture => new Variation(culture, null))
                .ToArray();
    }
}
