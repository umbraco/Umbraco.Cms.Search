using Microsoft.Extensions.DependencyInjection;
using Package.Helpers;
using Package.NotificationHandlers;
using Package.PropertyValueHandlers;
using Package.PropertyValueHandlers.Collection;
using Package.Services.ContentIndexing;
using Package.Services.ContentIndexing.Indexers;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;

namespace Package.DependencyInjection;

public sealed class ContentIndexingComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddTransient<IContentIndexingService, ContentIndexingService>();
        builder.Services.AddTransient<IContentIndexingDataCollectionService, ContentIndexingDataCollectionService>();

        builder.Services.AddTransient<IContentIndexer, SystemFieldsContentIndexer>();
        builder.Services.AddTransient<IContentIndexer, PropertyValueFieldsContentIndexer>();

        builder.Services.AddTransient<IDateTimeOffsetConverter, DateTimeOffsetConverter>();

        builder.AddNotificationAsyncHandler<ContentCacheRefresherNotification, ContentIndexingNotificationHandler>();

        builder
            .WithCollectionBuilder<PropertyValueHandlerCollectionBuilder>()
            .Add(() => builder.TypeLoader.GetTypes<IPropertyValueHandler>());
    }
}