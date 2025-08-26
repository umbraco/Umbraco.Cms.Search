using Umbraco.Cms.Search.Core.Services;

namespace Umbraco.Cms.Search.Provider.InMemory.Services;

// public marker interface allowing for explicit index registrations using the in-memory indexer
public interface IInMemoryIndexer : IIndexer
{
}
