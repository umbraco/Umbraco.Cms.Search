using Umbraco.Cms.Search.Core.Services;

namespace Umbraco.Cms.Search.Provider.InMemory.Services;

// public marker interface allowing for explicit index registrations using the in-memory searcher 
public interface IInMemorySearcher : ISearcher
{
    
}