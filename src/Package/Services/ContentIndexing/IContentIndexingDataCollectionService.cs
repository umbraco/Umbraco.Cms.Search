using Package.Models.Indexing;
using Umbraco.Cms.Core.Models;

namespace Package.Services.ContentIndexing;

public interface IContentIndexingDataCollectionService
{
    Task<IEnumerable<IndexField>?> CollectAsync(IContent content, bool published, CancellationToken cancellationToken);
}