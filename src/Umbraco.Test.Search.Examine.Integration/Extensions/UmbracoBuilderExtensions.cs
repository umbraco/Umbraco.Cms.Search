﻿using Examine.Lucene.Directories;
using Examine.Lucene.Providers;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.Test.Search.Examine.Integration.Extensions;

internal static class UmbracoBuilderExtensions
{
    public static IUmbracoBuilder AddExamineSearchProviderForTest<TIndex, TDirectoryFactory>(this IUmbracoBuilder builder)
        where TIndex : LuceneIndex
        where TDirectoryFactory : class, IDirectoryFactory
    {
        builder.Services.AddExamineSearchProviderServicesForTest<TIndex, TDirectoryFactory>();

        builder.AddNotificationHandler<ContentTreeChangeNotification, ContentTreeChangeDistributedCacheNotificationHandler>();
        builder.AddNotificationHandler<MediaTreeChangeNotification, MediaTreeChangeDistributedCacheNotificationHandler>();
        builder.AddNotificationHandler<MemberSavedNotification, MemberSavedDistributedCacheNotificationHandler>();
        builder.AddNotificationHandler<MemberDeletedNotification, MemberDeletedDistributedCacheNotificationHandler>();

        return builder;
    }
}
