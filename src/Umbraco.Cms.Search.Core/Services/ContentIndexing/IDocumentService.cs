using Umbraco.Cms.Search.Core.Models.Persistence;

namespace Umbraco.Cms.Search.Core.Services.ContentIndexing;

public interface IDocumentService
{
    Task<Document?> GetAsync(Guid id, string indexAlias);

    Task<IReadOnlyDictionary<Guid, Document>> GetManyAsync(IEnumerable<Guid> ids, string indexAlias);

    Task AddAsync(Document document);

    Task DeleteAsync(Guid id, string indexAlias);
}
