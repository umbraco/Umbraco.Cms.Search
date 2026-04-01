using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.HostedServices;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Search.Core.Configuration;
using Umbraco.Cms.Search.Core.Models.Configuration;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Notifications;
using Umbraco.Extensions;

namespace Umbraco.Cms.Search.Core.Services.ContentIndexing;

internal sealed class ContentIndexingService : IContentIndexingService
{
    private readonly IBackgroundTaskQueue _backgroundTaskQueue;
    private readonly IEventAggregator _eventAggregator;
    private readonly ILogger<ContentIndexingService> _logger;
    private readonly IndexOptions _indexOptions;
    private readonly IServiceProvider _serviceProvider;
    private readonly IOriginProvider _originProvider;
    private readonly IIndexDocumentService _indexDocumentService;

    public ContentIndexingService(
        IBackgroundTaskQueue backgroundTaskQueue,
        IEventAggregator eventAggregator,
        ILogger<ContentIndexingService> logger,
        IOptions<IndexOptions> indexOptions,
        IServiceProvider serviceProvider,
        IOriginProvider originProvider,
        IIndexDocumentService indexDocumentService)
    {
        _backgroundTaskQueue = backgroundTaskQueue;
        _eventAggregator = eventAggregator;
        _logger = logger;
        _indexOptions = indexOptions.Value;
        _serviceProvider = serviceProvider;
        _originProvider = originProvider;
        _indexDocumentService = indexDocumentService;
    }

    public void Handle(IEnumerable<ContentChange> changes, string origin)
    {
        ContentChange[] changesAsArray = changes as ContentChange[] ?? changes.ToArray();

        var currentOrigin = _originProvider.GetCurrent();
        IEnumerable<IGrouping<Type, ContentIndexRegistration>> indexRegistrationsByStrategyType = _indexOptions
            .GetContentIndexRegistrations()
            .Where(registration => registration.SameOriginOnly is false || origin == currentOrigin)
            .GroupBy(r => r.ContentChangeStrategy);

        foreach (IGrouping<Type, ContentIndexRegistration> group in indexRegistrationsByStrategyType)
        {
            if (TryGetContentChangeStrategy(group.Key, out IContentChangeStrategy? contentChangeStrategy) is false)
            {
                continue;
            }

            ContentIndexInfo[] indexInfos = group
                .Select(g =>
                    TryGetIndexer(g.Indexer, out IIndexer? indexer)
                        ? new ContentIndexInfo(g.IndexAlias, g.ContainedObjectTypes, indexer)
                        : null)
                .WhereNotNull()
                .ToArray();

            if (indexInfos.Length == 0)
            {
                _logger.LogWarning($"Could not resolve any indexes for {nameof(IContentChangeStrategy)} of type {{type}}. Index updates will be skipped.", group.Key.FullName);
                continue;
            }

            _backgroundTaskQueue.QueueBackgroundWorkItem(async cancellationToken => await contentChangeStrategy.HandleAsync(indexInfos, changesAsArray, cancellationToken));
        }
    }

    public void Rebuild(string indexAlias, string origin)
    {
        ContentIndexRegistration? indexRegistration = _indexOptions.GetContentIndexRegistration(indexAlias);
        if (indexRegistration is null)
        {
            _logger.LogError("Cannot rebuild index - no index registration found for alias: {indexAlias}", indexAlias);
            return;
        }

        if (indexRegistration.SameOriginOnly && origin != _originProvider.GetCurrent())
        {
            return;
        }

        _backgroundTaskQueue.QueueBackgroundWorkItem(async cancellationToken => await RebuildAsync(indexRegistration, cancellationToken));
    }

    public void ReindexByContentTypes(Guid[] contentTypeKeys, UmbracoObjectTypes objectType, string origin)
    {
        Guid[] contentKeys = GetContentKeysByContentTypes(contentTypeKeys, objectType);
        if (contentKeys.Length == 0)
        {
            return;
        }

        FlushDocumentIndexCache(contentKeys);

        ContentChange[] changes = CreateContentChanges(contentKeys, objectType);
        Handle(changes, origin);
    }

    private Guid[] GetContentKeysByContentTypes(Guid[] contentTypeKeys, UmbracoObjectTypes objectType)
    {
        return objectType switch
        {
            UmbracoObjectTypes.Document => GetDocumentKeysByContentTypes(contentTypeKeys),
            UmbracoObjectTypes.Media => GetMediaKeysByMediaTypes(contentTypeKeys),
            UmbracoObjectTypes.Member => GetMemberKeysByMemberTypes(contentTypeKeys),
            _ => [],
        };
    }

    private Guid[] GetDocumentKeysByContentTypes(Guid[] contentTypeKeys)
    {
        IContentTypeService contentTypeService = _serviceProvider.GetRequiredService<IContentTypeService>();
        IContentService contentService = _serviceProvider.GetRequiredService<IContentService>();

        int[] directContentTypeIds = contentTypeKeys
            .Select(key => contentTypeService.Get(key))
            .Where(ct => ct is not null)
            .Select(ct => ct!.Id)
            .ToArray();

        if (directContentTypeIds.Length == 0)
        {
            return [];
        }

        int[] allContentTypeIds = ExpandWithDependentContentTypes(contentTypeService, directContentTypeIds);

        var keys = new List<Guid>();
        var pageIndex = 0L;

        while (true)
        {
            IContent[] page = contentService.GetPagedOfTypes(
                allContentTypeIds, pageIndex, 1000, out long totalRecords, null, null).ToArray();
            keys.AddRange(page.Select(c => c.Key));
            pageIndex++;

            if (keys.Count >= totalRecords)
            {
                break;
            }
        }

        return keys.ToArray();
    }

    private Guid[] GetMediaKeysByMediaTypes(Guid[] mediaTypeKeys)
    {
        IMediaTypeService mediaTypeService = _serviceProvider.GetRequiredService<IMediaTypeService>();
        IMediaService mediaService = _serviceProvider.GetRequiredService<IMediaService>();

        int[] directMediaTypeIds = mediaTypeKeys
            .Select(key => mediaTypeService.Get(key))
            .Where(mt => mt is not null)
            .Select(mt => mt!.Id)
            .ToArray();

        if (directMediaTypeIds.Length == 0)
        {
            return [];
        }

        int[] allMediaTypeIds = ExpandWithDependentContentTypes(mediaTypeService, directMediaTypeIds);

        var keys = new List<Guid>();
        var pageIndex = 0L;

        while (true)
        {
            IMedia[] page = mediaService.GetPagedOfTypes(
                allMediaTypeIds, pageIndex, 1000, out long totalRecords, null, null).ToArray();
            keys.AddRange(page.Select(m => m.Key));
            pageIndex++;

            if (keys.Count >= totalRecords)
            {
                break;
            }
        }

        return keys.ToArray();
    }

    private Guid[] GetMemberKeysByMemberTypes(Guid[] memberTypeKeys)
    {
        IMemberTypeService memberTypeService = _serviceProvider.GetRequiredService<IMemberTypeService>();
        IMemberService memberService = _serviceProvider.GetRequiredService<IMemberService>();

        int[] directMemberTypeIds = memberTypeKeys
            .Select(key => memberTypeService.Get(key))
            .Where(mt => mt is not null)
            .Select(mt => mt!.Id)
            .ToArray();

        if (directMemberTypeIds.Length == 0)
        {
            return [];
        }

        int[] allMemberTypeIds = ExpandWithDependentContentTypes(memberTypeService, directMemberTypeIds);

        var keys = new List<Guid>();
        foreach (int memberTypeId in allMemberTypeIds)
        {
            IEnumerable<IMember> members = memberService.GetMembersByMemberType(memberTypeId);
            keys.AddRange(members.Select(m => m.Key));
        }

        return keys.ToArray();
    }

    private static int[] ExpandWithDependentContentTypes<T>(IContentTypeBaseService<T> contentTypeService, int[] contentTypeIds)
        where T : IContentTypeComposition
    {
        T[] allTypes = contentTypeService.GetAll().ToArray();
        var result = new HashSet<int>(contentTypeIds);

        int previousCount;
        do
        {
            previousCount = result.Count;
            foreach (T ct in allTypes)
            {
                if (result.Contains(ct.Id) is false && ct.CompositionIds().Any(result.Contains))
                {
                    result.Add(ct.Id);
                }
            }
        }
        while (result.Count > previousCount);

        return result.ToArray();
    }

    private void FlushDocumentIndexCache(Guid[] contentKeys)
    {
        _indexDocumentService.DeleteAsync(contentKeys, true).GetAwaiter().GetResult();
        _indexDocumentService.DeleteAsync(contentKeys, false).GetAwaiter().GetResult();
    }

    private static ContentChange[] CreateContentChanges(Guid[] contentKeys, UmbracoObjectTypes objectType)
    {
        return objectType switch
        {
            UmbracoObjectTypes.Document => contentKeys
                .SelectMany(key => new[]
                {
                    ContentChange.Document(key, ChangeImpact.Refresh, ContentState.Draft),
                    ContentChange.Document(key, ChangeImpact.Refresh, ContentState.Published),
                })
                .ToArray(),
            UmbracoObjectTypes.Media => contentKeys
                .Select(key => ContentChange.Media(key, ChangeImpact.Refresh, ContentState.Draft))
                .ToArray(),
            UmbracoObjectTypes.Member => contentKeys
                .Select(key => ContentChange.Member(key, ChangeImpact.Refresh, ContentState.Draft))
                .ToArray(),
            _ => [],
        };
    }

    private async Task RebuildAsync(ContentIndexRegistration indexRegistration, CancellationToken cancellationToken)
    {
        if (TryGetContentChangeStrategy(indexRegistration.ContentChangeStrategy, out IContentChangeStrategy? contentChangeStrategy) is false
            || TryGetIndexer(indexRegistration.Indexer, out IIndexer? indexer) is false)
        {
            return;
        }

        await _eventAggregator.PublishAsync(new IndexRebuildStartingNotification(indexRegistration.IndexAlias), cancellationToken);

        await contentChangeStrategy.RebuildAsync(new ContentIndexInfo(indexRegistration.IndexAlias, indexRegistration.ContainedObjectTypes, indexer), cancellationToken);

        await _eventAggregator.PublishAsync(new IndexRebuildCompletedNotification(indexRegistration.IndexAlias), cancellationToken);
    }

    private bool TryGetContentChangeStrategy(Type type, [NotNullWhen(true)] out IContentChangeStrategy? contentChangeStrategy)
    {
        if (_serviceProvider.GetService(type) is IContentChangeStrategy resolvedContentChangeStrategy)
        {
            contentChangeStrategy = resolvedContentChangeStrategy;
            return true;
        }

        _logger.LogError($"Could not resolve type {{type}} as {nameof(IContentChangeStrategy)}. Make sure the type is registered in the DI.", type.FullName);
        contentChangeStrategy = null;
        return false;
    }

    private bool TryGetIndexer(Type type, [NotNullWhen(true)] out IIndexer? indexer)
    {
        if (_serviceProvider.GetService(type) is IIndexer resolvedIndexer)
        {
            indexer = resolvedIndexer;
            return true;
        }

        _logger.LogError($"Could not resolve type {{type}} as {nameof(IIndexer)}. Make sure the type is registered in the DI.", type.FullName);
        indexer = null;
        return false;
    }
}
