using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Search.Core.Cache;
using Umbraco.Cms.Search.Core.Cache.Content;
using Umbraco.Cms.Search.Core.Helpers;
using Umbraco.Cms.Search.Core.NotificationHandlers;
using Umbraco.Cms.Search.Core.PropertyValueHandlers;
using Umbraco.Cms.Search.Core.PropertyValueHandlers.Collection;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;
using Umbraco.Cms.Search.Core.Services.ContentIndexing.Indexers;
using PublicAccessCacheRefresherNotification = Umbraco.Cms.Search.Core.Cache.PublicAccess.PublicAccessCacheRefresherNotification;

namespace Umbraco.Cms.Search.Core.DependencyInjection;

public static class UmbracoBuilderExtensions
{
    public static IUmbracoBuilder AddSearchCore(this IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IContentIndexingService, ContentIndexingService>();

        builder.Services.AddTransient<IContentIndexingDataCollectionService, ContentIndexingDataCollectionService>();

        builder.Services.AddTransient<IContentIndexer, SystemFieldsContentIndexer>();
        builder.Services.AddTransient<IContentIndexer, PropertyValueFieldsContentIndexer>();

        builder.Services.AddTransient<IDateTimeOffsetConverter, DateTimeOffsetConverter>();
        builder.Services.AddTransient<IContentProtectionProvider, ContentProtectionProvider>();

        builder.Services.AddTransient<PublishedContentChangeStrategy>();
        builder.Services.AddTransient<DraftContentChangeStrategy>();

        builder.Services.AddTransient<IPublishedContentChangeStrategy, PublishedContentChangeStrategy>();
        builder.Services.AddTransient<IDraftContentChangeStrategy, DraftContentChangeStrategy>();

        builder
            .AddNotificationAsyncHandler<ContentCacheRefresherNotification, ContentIndexingNotificationHandler>()
            .AddNotificationAsyncHandler<PublicAccessCacheRefresherNotification, PublicAccessIndexingNotificationHandler>();

        builder
            .WithCollectionBuilder<PropertyValueHandlerCollectionBuilder>()
            .Add(() => builder.TypeLoader.GetTypes<IPropertyValueHandler>());

        builder.AddDistributedCacheForSearch();
        
        return builder;
    }
}