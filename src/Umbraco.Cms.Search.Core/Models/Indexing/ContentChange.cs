using Umbraco.Cms.Core.Models;

namespace Umbraco.Cms.Search.Core.Models.Indexing;

public record ContentChange
{
    private ContentChange(Guid key, UmbracoObjectTypes objectType, ChangeImpact changeImpact, ContentState contentState)
    {
        Key = key;
        ObjectType = objectType;
        ChangeImpact = changeImpact;
        ContentState = contentState;
    }
    
    public static ContentChange Document(Guid key, ChangeImpact changeImpact, ContentState contentState)
        => new (key, UmbracoObjectTypes.Document, changeImpact, contentState);

    public static ContentChange Media(Guid key, ChangeImpact changeImpact, ContentState contentState)
        => new (key, UmbracoObjectTypes.Media, changeImpact, contentState);

    public Guid Key { get; }

    public UmbracoObjectTypes ObjectType { get; }

    public ChangeImpact ChangeImpact { get; }

    public ContentState ContentState { get; }
}