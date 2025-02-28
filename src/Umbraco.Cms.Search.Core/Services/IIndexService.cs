using Umbraco.Cms.Search.Core.Models.Indexing;

namespace Umbraco.Cms.Search.Core.Services;

public interface IIndexService
{
    Task AddOrUpdateAsync(Guid key, string stamp, IEnumerable<Variation> variations, IEnumerable<IndexField> fields);

    Task DeleteAsync(IEnumerable<Guid> keys);

    Task<string?> GetStampAsync(Guid key);
}