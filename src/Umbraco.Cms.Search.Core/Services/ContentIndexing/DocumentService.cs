using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Search.Core.Models.Persistence;
using Umbraco.Cms.Search.Core.Persistence;

namespace Umbraco.Cms.Search.Core.Services.ContentIndexing;

public class DocumentService : IDocumentService
{
    private readonly ICoreScopeProvider _scopeProvider;
    private readonly IDocumentRepository _documentRepository;

    public DocumentService(ICoreScopeProvider scopeProvider, IDocumentRepository documentRepository)
    {
        _scopeProvider = scopeProvider;
        _documentRepository = documentRepository;
    }

    public async Task<Document?> GetAsync(Guid id, string indexAlias)
    {
        using ICoreScope scope = _scopeProvider.CreateCoreScope();
        Document? document = await _documentRepository.GetAsync(id, indexAlias);
        scope.Complete();
        return document;
    }

    public async Task<IReadOnlyDictionary<Guid, Document>> GetManyAsync(IEnumerable<Guid> ids, string indexAlias)
    {
        using ICoreScope scope = _scopeProvider.CreateCoreScope();
        IReadOnlyDictionary<Guid, Document> documents = await _documentRepository.GetManyAsync(ids, indexAlias);
        scope.Complete();
        return documents;
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
}
