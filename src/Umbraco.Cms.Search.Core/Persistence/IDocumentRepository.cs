using Umbraco.Cms.Search.Core.Models.Persistence;

namespace Umbraco.Cms.Search.Core.Persistence;

public interface IDocumentRepository
{
    public Task AddAsync(Document document);

    public Task<Document?> GetAsync(Guid id, string indexAlias);

    public Task DeleteAsync(Guid id, string indexAlias);
}
