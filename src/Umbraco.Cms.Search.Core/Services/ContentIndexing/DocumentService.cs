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

    public async Task AddAsync(Document document)
    {
        using ICoreScope scope = _scopeProvider.CreateCoreScope();
        await _documentRepository.AddAsync(document);
        scope.Complete();
    }

    public async Task DeleteAsync(Guid[] ids, bool published)
    {
        using ICoreScope scope = _scopeProvider.CreateCoreScope();
        await _documentRepository.DeleteAsync(ids, published);
        scope.Complete();
    }

    public async Task<Document?> GetAsync(Guid id, bool published)
    {
        using ICoreScope scope = _scopeProvider.CreateCoreScope();
        Document? document = await _documentRepository.GetAsync(id, published);
        scope.Complete();

        return document;
    }

    public async Task DeleteAllAsync()
    {
        using ICoreScope scope = _scopeProvider.CreateCoreScope();
        await _documentRepository.DeleteAllAsync();
        scope.Complete();
    }
}
