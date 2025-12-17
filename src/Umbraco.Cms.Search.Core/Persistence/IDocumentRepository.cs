using Umbraco.Cms.Search.Core.Models.Persistence;

namespace Umbraco.Cms.Search.Core.Persistence;

public interface IDocumentRepository
{
    public Task AddAsync(Document document, string changeStrategy);

    public Task<Document?> GetAsync(Guid id, string changeStrategy);

    public Task DeleteAsync(Guid id, string changeStrategy);

    public Task<IEnumerable<Document>> GetByChangeStrategyAsync(string changeStrategy);
}
