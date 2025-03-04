using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.Cms.Search.Core.Cache;

/*
 * The Umbraco core distributed caching for public access changes is lacking detail; it ever only broadcasts that
 * "something has changed - please refresh everything". While that works for the core to invalidate any caching of
 * public access configuration entries, it does not work for search: it's too costly to "just refresh everything".
 * 
 * This sets up a custom cache refresher for public access changes with a better granularity, which means we can
 * make an informed decision of how much to re-index when public access changes occur.
 *
 * Eventually the core should be retrofitted with a more granular handling. When that happens, everything in this
 * namespace can be deleted.
 */
public class PublicAccessCacheComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AddNotificationHandler<PublicAccessEntrySavedNotification, PublicAccessEntrySavedNotificationHandler>();
        builder.AddNotificationHandler<PublicAccessEntryDeletedNotification, PublicAccessEntryDeletedNotificationHandler>();
    }
}