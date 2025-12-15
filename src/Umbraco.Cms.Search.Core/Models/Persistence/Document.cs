using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Indexing;

namespace Umbraco.Cms.Search.Core.Models.Persistence;

public class Document
{
    public required Guid DocumentKey { get; set; }

    public required IndexField[] Fields { get; set; }

    public UmbracoObjectTypes ObjectType { get; set; }

    public required Variation[] Variations { get; set; }

    public ContentProtection? Protection { get; set; }
}
