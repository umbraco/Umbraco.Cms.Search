using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Indexing;

namespace Umbraco.Cms.Search.Core.Services;

public sealed class NoopIndexService : IIndexService 
{
    public Task AddOrUpdateAsync(string indexAlias, Guid id, UmbracoObjectTypes objectType, IEnumerable<Variation> variations, IEnumerable<IndexField> fields, ContentProtection? protection)
        => Task.CompletedTask;

    public Task DeleteAsync(string indexAlias, IEnumerable<Guid> ids)
        => Task.CompletedTask;
}