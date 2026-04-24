namespace Umbraco.Cms.Search.Core.Models.Searching;

public record AccessContext(Guid PrincipalId, Guid[]? GroupIds, bool Ignore = false)
{
    public static AccessContext Ignored() => new(Guid.Empty, null, true);
}
