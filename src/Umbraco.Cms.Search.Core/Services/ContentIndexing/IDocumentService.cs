using Umbraco.Cms.Search.Core.Models.Persistence;

namespace Umbraco.Cms.Search.Core.Services.ContentIndexing;

public interface IDocumentService
{
    Task AddAsync(Document document);

    Task DeleteAsync(Guid[] ids, bool published);

    Task<Document?> GetAsync(Guid id, bool published);

    Task DeleteAllAsync();
}
