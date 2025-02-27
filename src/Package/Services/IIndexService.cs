using Package.Models.Indexing;

namespace Package.Services;

public interface IIndexService
{
    Task AddOrUpdateAsync(Guid key, string stamp, IEnumerable<Variation> variations, IEnumerable<IndexField> fields);

    Task DeleteAsync(IEnumerable<Guid> keys);

    Task<string?> GetStampAsync(Guid key);
}