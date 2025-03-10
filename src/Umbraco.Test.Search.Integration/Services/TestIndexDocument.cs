using Umbraco.Cms.Search.Core.Models.Indexing;

namespace Umbraco.Test.Search.Integration.Services;

public record TestIndexDocument(Guid Key, IEnumerable<Variation> Variations, IEnumerable<IndexField> Fields, ContentProtection? Protection)
{
}