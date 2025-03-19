using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Indexing;

namespace Umbraco.Test.Search.Integration.Services;

public record TestIndexDocument(Guid Key, UmbracoObjectTypes? ObjectType, IEnumerable<Variation> Variations, IEnumerable<IndexField> Fields, ContentProtection? Protection)
{
}