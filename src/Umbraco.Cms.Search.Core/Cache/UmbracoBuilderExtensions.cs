using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Search.Core.Cache.Content;
using Umbraco.Cms.Search.Core.Cache.PublicAccess;

namespace Umbraco.Cms.Search.Core.Cache;

/*
 * This wires up custom distributed cache refreshers for content and public access changes.
 *
 * Eventually the core cache refreshers should be retrofitted with a more granularity. When that happens, everything
 * in this namespace can be removed.
 *
 * ## Content cache refresher ##
 *
 * The core distributed caching for content changes cannot tell the difference between "something was published" and
 * "something was saved". We need that to perform only the indexing operations strictly necessary when maintaining
 * indexes for published and draft content, respectively.
 *
 * This custom cache refresher adds that level of granularity. It also adds the ability to distinguish between
 * "publish" and "republish" at culture level, because we only want to trigger a full reindex of all descendants
 * in a given culture (or invariant) when publishing - not when republishing the same culture.
 *
 * ## Public access cache refresher ##
 *
 * The core distributed caching for public access changes is lacking detail; it ever only broadcasts that
 * "something has changed - please refresh everything". While that works for the core to invalidate any caching of
 * public access configuration entries, it does not work for search: it's too costly to "just refresh everything".
 *
 * This custom cache refresher for public access changes has a better granularity, which means we can
 * make an informed decision of how much to re-index when public access changes occur.
 *
 */
public static class UmbracoBuilderExtensions
{
    public static IUmbracoBuilder AddDistributedCacheForSearch(this IUmbracoBuilder builder)
    {
        builder.AddNotificationHandler<ContentPublishingNotification, PublishNotificationHandler>();
        builder.AddNotificationHandler<ContentPublishedNotification, PublishNotificationHandler>();
        builder.AddNotificationHandler<ContentUnpublishedNotification, PublishNotificationHandler>();
        builder.AddNotificationHandler<ContentMovedToRecycleBinNotification, PublishNotificationHandler>();

        builder.AddNotificationHandler<PublicAccessEntrySavedNotification, PublicAccessNotificationHandler>();
        builder.AddNotificationHandler<PublicAccessEntryDeletedNotification, PublicAccessNotificationHandler>();

        return builder;
    }
}