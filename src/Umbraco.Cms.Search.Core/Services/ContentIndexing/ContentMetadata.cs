using System.Runtime.Serialization;

namespace Umbraco.Cms.Search.Core.Services.ContentIndexing;

[DataContract]
// NOTE: must be public for serialization purposes
public class ContentMetadata
{
    [DataMember]
    public ContentMetadataVariation[] Variations { get; init; } = [];
}