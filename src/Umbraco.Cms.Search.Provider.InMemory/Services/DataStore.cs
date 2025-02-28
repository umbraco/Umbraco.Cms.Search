using Umbraco.Cms.Search.Provider.InMemory.Models;

namespace Umbraco.Cms.Search.Provider.InMemory.Services;

internal class DataStore : Dictionary<Guid, IndexDocument>
{
}