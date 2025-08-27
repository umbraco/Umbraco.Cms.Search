using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Indexing;

namespace Umbraco.Cms.Search.Core.Services;

internal sealed class NoopIndexer : IIndexer
{
    public Task AddOrUpdateAsync(string indexAlias, Guid id, UmbracoObjectTypes objectType, IEnumerable<Variation> variations, IEnumerable<IndexField> fields, ContentProtection? protection)
        => Task.CompletedTask;

    public Task DeleteAsync(string indexAlias, IEnumerable<Guid> ids)
        => Task.CompletedTask;

    public Task ResetAsync(string indexAlias)
        => Task.CompletedTask;
}
