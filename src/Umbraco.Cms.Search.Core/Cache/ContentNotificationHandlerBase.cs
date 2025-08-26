using Umbraco.Cms.Core.Models;

namespace Umbraco.Cms.Search.Core.Cache;

internal abstract class ContentNotificationHandlerBase
{
    protected T[] FindTopmostEntities<T>(IEnumerable<T> candidates)
        where T : IContentBase
    {
        T[] candidatesAsArray = candidates as T[] ?? candidates.ToArray();
        var ids = candidatesAsArray.Select(entity => entity.Id).ToArray();
        return candidatesAsArray.Where(entity => ids.Contains(entity.ParentId) is false).ToArray();
    }
}
