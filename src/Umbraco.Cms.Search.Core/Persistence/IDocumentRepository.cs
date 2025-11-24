using Umbraco.Cms.Search.Core.Models.Persistence;

namespace Umbraco.Cms.Search.Core.Persistence;

public interface IDocumentRepository
{
    public Task Add(Document document);

    public Task<Document> Get(Guid id, string indexAlias);

    public Task Remove(Guid id);
}
