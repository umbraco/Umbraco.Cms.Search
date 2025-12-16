using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Search.Core.Models.Persistence;
using Umbraco.Cms.Search.Core.Persistence;

namespace Umbraco.Cms.Search.Core.Services.ContentIndexing;

public class DocumentService : IDocumentService
{
    private readonly ICoreScopeProvider _scopeProvider;
    private readonly IDocumentRepository _documentRepository;

    public DocumentService(ICoreScopeProvider scopeProvider, IDocumentRepository documentRepository, IContentIndexingDataCollectionService contentIndexingDataCollectionService, IContentProtectionProvider contentProtectionProvider, IContentService contentService)
    {
        _scopeProvider = scopeProvider;
        _documentRepository = documentRepository;
    }

    public async Task AddAsync(Document document, string changeStrategy)
    {
        using ICoreScope scope = _scopeProvider.CreateCoreScope();
        await _documentRepository.AddAsync(document, changeStrategy);
        scope.Complete();
    }

    public async Task DeleteAsync(Guid id, string changeStrategy)
    {
        using ICoreScope scope = _scopeProvider.CreateCoreScope();
        await _documentRepository.DeleteAsync(id, changeStrategy);
        scope.Complete();
    }

    public async Task<IEnumerable<Document>> GetByChangeStrategyAsync(string changeStrategy)
    {
        using ICoreScope scope = _scopeProvider.CreateCoreScope();
        IEnumerable<Document> documents = await _documentRepository.GetByChangeStrategyAsync(changeStrategy);
        scope.Complete();
        return documents;
    }
}
