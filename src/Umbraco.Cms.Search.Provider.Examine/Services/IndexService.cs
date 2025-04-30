using Examine;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Services;

namespace Umbraco.Cms.Search.Provider.Examine.Services;

public class IndexService : IIndexService
{
    public IndexService(IExamineManager examineManager)
    {
    }
    public Task AddOrUpdateAsync(string indexAlias, Guid key, UmbracoObjectTypes objectType, IEnumerable<Variation> variations,
        IEnumerable<IndexField> fields, ContentProtection? protection)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(string indexAlias, IEnumerable<Guid> keys)
    {
        throw new NotImplementedException();
    }
}