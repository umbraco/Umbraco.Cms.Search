using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Search.Core.Models.Persistence;
using Umbraco.Cms.Search.Core.Persistence;

namespace Umbraco.Cms.Search.Core.Services.ContentIndexing;

public class IndexDocumentService : IIndexDocumentService
{
    private readonly ICoreScopeProvider _scopeProvider;
    private readonly IIndexDocumentRepository _indexDocumentRepository;

    public IndexDocumentService(ICoreScopeProvider scopeProvider, IIndexDocumentRepository indexDocumentRepository)
    {
        _scopeProvider = scopeProvider;
        _indexDocumentRepository = indexDocumentRepository;
    }

    public async Task AddAsync(IndexDocument indexDocument)
    {
        using ICoreScope scope = _scopeProvider.CreateCoreScope();
        await _indexDocumentRepository.AddAsync(indexDocument);
        scope.Complete();
    }

    public async Task DeleteAsync(Guid[] ids, bool published)
    {
        using ICoreScope scope = _scopeProvider.CreateCoreScope();
        await _indexDocumentRepository.DeleteAsync(ids, published);
        scope.Complete();
    }

    public async Task<IndexDocument?> GetAsync(Guid id, bool published)
    {
        using ICoreScope scope = _scopeProvider.CreateCoreScope();
        IndexDocument? document = await _indexDocumentRepository.GetAsync(id, published);
        scope.Complete();

        return document;
    }

    public async Task DeleteAllAsync()
    {
        using ICoreScope scope = _scopeProvider.CreateCoreScope();
        await _indexDocumentRepository.DeleteAllAsync();
        scope.Complete();
    }
}
