using Umbraco.Cms.Search.Core.Models.Indexing;

namespace Umbraco.Test.Search.Integration.Services;

public record TestIndexDocument(Guid Key, string Stamp, IEnumerable<Variation> Variations, IEnumerable<IndexField> Fields)
{
}