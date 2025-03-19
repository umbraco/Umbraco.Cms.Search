using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Indexing;

namespace Umbraco.Cms.Search.Provider.InMemory.Models;

internal record IndexDocument(UmbracoObjectTypes ObjectType, Variation[] Variations, IndexField[] Fields, ContentProtection? Protection)
{
}
