using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Search.Core.Helpers;
using Umbraco.Cms.Search.Core.NotificationHandlers;
using Umbraco.Cms.Search.Core.PropertyValueHandlers;
using Umbraco.Cms.Search.Core.PropertyValueHandlers.Collection;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;
using Umbraco.Cms.Search.Core.Services.ContentIndexing.Indexers;

using PublicAccessCacheRefresherNotification = Umbraco.Cms.Search.Core.Cache.PublicAccessCacheRefresherNotification;

namespace Umbraco.Cms.Search.Core.DependencyInjection;

public sealed class ContentIndexingComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddTransient<IContentIndexingService, ContentIndexingService>();
        builder.Services.AddTransient<IContentIndexingDataCollectionService, ContentIndexingDataCollectionService>();

        builder.Services.AddTransient<IContentIndexer, SystemFieldsContentIndexer>();
        builder.Services.AddTransient<IContentIndexer, PropertyValueFieldsContentIndexer>();

        builder.Services.AddTransient<IDateTimeOffsetConverter, DateTimeOffsetConverter>();
        builder.Services.AddTransient<IContentProtectionProvider, ContentProtectionProvider>();

        builder
            .AddNotificationAsyncHandler<ContentCacheRefresherNotification, ContentIndexingNotificationHandler>()
            .AddNotificationAsyncHandler<PublicAccessCacheRefresherNotification, PublicAccessIndexingNotificationHandler>();

        builder
            .WithCollectionBuilder<PropertyValueHandlerCollectionBuilder>()
            .Add(() => builder.TypeLoader.GetTypes<IPropertyValueHandler>());
    }
}