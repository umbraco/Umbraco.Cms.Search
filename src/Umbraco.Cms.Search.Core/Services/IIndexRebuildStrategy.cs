using Umbraco.Cms.Search.Core.Models.Indexing;

namespace Umbraco.Cms.Search.Core.Services;

public interface IIndexRebuildStrategy
{
    Task RebuildAsync(IndexInfo indexInfo, CancellationToken cancellationToken);
}
