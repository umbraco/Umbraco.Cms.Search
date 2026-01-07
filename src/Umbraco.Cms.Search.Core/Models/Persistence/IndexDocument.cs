using Umbraco.Cms.Search.Core.Models.Indexing;

namespace Umbraco.Cms.Search.Core.Models.Persistence;

public class IndexDocument
{
    public required Guid DocumentKey { get; set; }

    public required IndexField[] Fields { get; set; }

    public required bool Published { get; set; }
}
