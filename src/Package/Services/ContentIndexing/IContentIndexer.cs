using Package.Models.Indexing;
using Umbraco.Cms.Core.Models;

namespace Package.Services.ContentIndexing;

public interface IContentIndexer
{
    Task<IEnumerable<IndexField>> GetIndexFieldsAsync(IContent content, string?[] cultures, bool published, CancellationToken cancellationToken);
}