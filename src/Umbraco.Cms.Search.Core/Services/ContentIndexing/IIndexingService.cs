using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Indexing;

namespace Umbraco.Cms.Search.Core.Services.ContentIndexing;

public interface IIndexingService
{
    Task<bool> IndexContentAsync(IndexInfo[] indexInfos, IContentBase content, string changeStrategy,  bool published, CancellationToken cancellationToken);
}
