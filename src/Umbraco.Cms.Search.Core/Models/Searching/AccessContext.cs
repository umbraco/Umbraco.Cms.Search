namespace Umbraco.Cms.Search.Core.Models.Searching;

public record AccessContext(Guid PrincipalId, Guid[]? GroupIds)
{
}
