using Umbraco.Cms.Search.Core.Models.Configuration;
using Umbraco.Cms.Search.Core.Services;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;

namespace Umbraco.Cms.Search.Core.Configuration;

public class IndexOptions
{
    private readonly Dictionary<string, (Type IndexService, Type ContentChangeStrategy)> _register = [];

    public void RegisterIndex<TIndexService, TContentChangeStrategy>(string indexAlias)
        where TIndexService : class, IIndexService
        where TContentChangeStrategy : class, IContentChangeStrategy
    {
        // TODO: detect collisions and throw (or log?)
        _register[indexAlias] = (typeof(TIndexService), typeof(TContentChangeStrategy));
    }

    internal IndexRegistration[] GetIndexRegistrations()
        => _register
            .Select(r => new IndexRegistration(r.Key, r.Value.IndexService, r.Value.ContentChangeStrategy))
            .ToArray();    
}