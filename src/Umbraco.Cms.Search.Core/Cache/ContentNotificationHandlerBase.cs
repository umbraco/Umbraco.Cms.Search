using Umbraco.Cms.Core.Models;

// NOTE: the namespace is defined as what it would be, if this was part of Umbraco core.
namespace Umbraco.Cms.Core.Events;

internal abstract class ContentNotificationHandlerBase
{
    protected T[] FindTopmostEntities<T>(IEnumerable<T> candidates)
        where T : IContentBase
    {
        var candidatesAsArray = candidates as T[] ?? candidates.ToArray();
        var ids = candidatesAsArray.Select(entity => entity.Id).ToArray();
        return candidatesAsArray.Where(entity => ids.Contains(entity.ParentId) is false).ToArray();
    }
}