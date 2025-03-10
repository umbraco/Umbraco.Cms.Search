namespace Umbraco.Cms.Search.Core.Models.Indexing;

public record ContentChange(Guid Key, ContentChangeType ChangeType, bool PublishStateAffected)
{
}