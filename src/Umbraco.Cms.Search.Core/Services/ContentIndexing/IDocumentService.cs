using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Persistence;

namespace Umbraco.Cms.Search.Core.Services.ContentIndexing;

public interface IDocumentService
{
    Task<Document?> GetAsync(IContentBase content, string indexAlias, bool published, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, Document>> GetManyAsync(IEnumerable<IContentBase> contents, string indexAlias, bool published, CancellationToken cancellationToken);

    Task AddAsync(Document document);

    Task DeleteAsync(Guid id, string indexAlias);

    Task<IEnumerable<Document>> GetByIndexAliasAsync(string indexAlias);
}
