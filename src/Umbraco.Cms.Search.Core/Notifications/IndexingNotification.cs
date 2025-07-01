using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Search.Core.Models.Indexing;

namespace Umbraco.Cms.Search.Core.Notifications;

public class IndexingNotification : ICancelableNotification
{
    public IndexingNotification(
        IndexInfo indexInfo,
        Guid id,
        UmbracoObjectTypes objectType,
        IEnumerable<Variation> variations,
        IEnumerable<IndexField> fields)
    {
        IndexInfo = indexInfo;
        Id = id;
        ObjectType = objectType;
        Variations = variations;
        Fields = fields;
    }

    public IndexInfo IndexInfo { get; }

    public Guid Id { get; }

    public UmbracoObjectTypes ObjectType { get; }

    public IEnumerable<Variation> Variations { get; }

    public IEnumerable<IndexField> Fields { get; set; }
    
    public bool Cancel { get; set; }
}