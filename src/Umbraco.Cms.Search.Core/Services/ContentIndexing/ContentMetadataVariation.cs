using System.Runtime.Serialization;

namespace Umbraco.Cms.Search.Core.Services.ContentIndexing;

[DataContract]
// NOTE: must be public for serialization purposes
public class ContentMetadataVariation : IEquatable<ContentMetadataVariation>
{
    [DataMember]
    public string? Culture { get; init; }

    [DataMember]
    public string? Segment { get; init; }

    public bool Equals(ContentMetadataVariation? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Culture == other.Culture && Segment == other.Segment;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != this.GetType())
        {
            return false;
        }

        return Equals((ContentMetadataVariation)obj);
    }

    public override int GetHashCode() => HashCode.Combine(Culture, Segment);
}