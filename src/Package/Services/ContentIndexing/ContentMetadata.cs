using System.Runtime.Serialization;

namespace Package.Services.ContentIndexing;

[DataContract]
// NOTE: must be public for serialization purposes
public class ContentMetadata
{
    [DataMember]
    public ContentMetadataVariation[] Variations { get; init; } = [];
}