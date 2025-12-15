using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Persistence;

namespace Umbraco.Cms.Search.Core.Services.ContentIndexing;

public interface IDocumentService
{
    Task<Document?> CalculateDocument(IContentBase content, bool published, CancellationToken cancellationToken);

    Task AddAsync(Document document, string changeStrategy);

    Task DeleteAsync(Guid id, string changeStrategy);

    Task<IEnumerable<Document>> GetByChangeStrategyAsync(string changeStrategy);
}
