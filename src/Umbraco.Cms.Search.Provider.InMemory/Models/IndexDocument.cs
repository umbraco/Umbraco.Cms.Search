using Umbraco.Cms.Search.Core.Models.Indexing;

namespace Umbraco.Cms.Search.Provider.InMemory.Models;

internal record IndexDocument(Variation[] Variations, IndexField[] Fields)
{
}
