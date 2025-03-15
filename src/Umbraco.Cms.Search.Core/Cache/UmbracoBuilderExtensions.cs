using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;

// NOTE: the namespace is defined as what it would be, if this was part of Umbraco core.
namespace Umbraco.Cms.Core.Events;

/*
 * This wires up notification handlers for custom distributed cache refreshers for content and public access changes.
 *
 * Eventually these cache refreshers should probably be added to core, or the core cache refreshers should be
 * retrofitted with a higher level of granularity.
 *
 * ## Published content cache refresher ##
 *
 * The core distributed caching for content changes cannot tell the difference between "something was published" and
 * "something was saved". We need that to perform only the indexing operations strictly necessary when maintaining
 * indexes for published and draft content, respectively.
 *
 * This custom cache refresher adds that level of granularity.
 *
 * It also adds the ability to distinguish between "publish" and "republish" at culture level, because we only want to
 * trigger a full reindex of all descendants in a given culture (or invariant) when (un)publishing a new culture - not
 * when republishing an already published culture.
 *
 * ## Detailed public access cache refresher ##
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
    public static IUmbracoBuilder AddCustomCacheRefresherNotificationHandlers(this IUmbracoBuilder builder)
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