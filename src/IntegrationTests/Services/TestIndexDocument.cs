using Package.Models.Indexing;

namespace IntegrationTests.Services;

public record TestIndexDocument(Guid Key, string Stamp, IEnumerable<Variation> Variations, IEnumerable<IndexField> Fields)
{
}