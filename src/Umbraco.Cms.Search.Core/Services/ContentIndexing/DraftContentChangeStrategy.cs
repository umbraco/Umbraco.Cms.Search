using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Models.Persistence;
using Umbraco.Cms.Search.Core.Notifications;

namespace Umbraco.Cms.Search.Core.Services.ContentIndexing;

internal sealed class DraftContentChangeStrategy : ContentChangeStrategyBase, IDraftContentChangeStrategy
{
    private readonly IContentService _contentService;
    private readonly IMediaService _mediaService;
    private readonly IMemberService _memberService;
    private readonly IEventAggregator _eventAggregator;
    private readonly IDocumentService _documentService;
    private readonly IIndexingService _indexingService;
    private const string StrategyName = "DraftContentChangeStrategy";

    protected override bool SupportsTrashedContent => true;

    public DraftContentChangeStrategy(
        IContentService contentService,
        IMediaService mediaService,
        IMemberService memberService,
        IEventAggregator eventAggregator,
        IDocumentService documentService,
        IUmbracoDatabaseFactory umbracoDatabaseFactory,
        IIdKeyMap idKeyMap,
        ILogger<DraftContentChangeStrategy> logger,
        IIndexingService indexingService)
        : base(umbracoDatabaseFactory, idKeyMap, logger)
    {
        _contentService = contentService;
        _mediaService = mediaService;
        _memberService = memberService;
        _eventAggregator = eventAggregator;
        _documentService = documentService;
        _indexingService = indexingService;
    }

    public async Task HandleAsync(IEnumerable<IndexInfo> indexInfos, IEnumerable<ContentChange> changes, CancellationToken cancellationToken)
    {
        IndexInfo[] indexInfosAsArray = indexInfos as IndexInfo[] ?? indexInfos.ToArray();

        // get the relevant changes for this change strategy
        ContentChange[] changesAsArray = changes.Where(change =>
                change.ContentState is ContentState.Draft
                && change.ObjectType is UmbracoObjectTypes.Document or UmbracoObjectTypes.Media or UmbracoObjectTypes.Member)
            .ToArray();

        var pendingRemovals = new List<ContentChange>();
        foreach (ContentChange change in changesAsArray.Where(change => change.ContentState is ContentState.Draft))
        {
            if (change.ChangeImpact is ChangeImpact.Remove)
            {
                pendingRemovals.Add(change);
            }
            else
            {
                IContentBase? content = GetContent(change);
                if (content is null)
                {
                    pendingRemovals.Add(change);
                    continue;
                }

                await RemoveFromIndexAsync(indexInfosAsArray, pendingRemovals);
                pendingRemovals.Clear();

                var updated = await HandleContentChangeAsync(indexInfosAsArray, change, content, cancellationToken);
                if (updated is false)
                {
                    pendingRemovals.Add(change);
                }
            }
        }

        await RemoveFromIndexAsync(indexInfosAsArray, pendingRemovals);
    }

    public async Task RebuildAsync(IndexInfo indexInfo, CancellationToken cancellationToken, bool useDatabase = false)
    {
        await indexInfo.Indexer.ResetAsync(indexInfo.IndexAlias);

        if (useDatabase)
        {
           IEnumerable<Document> documents = await _documentService.GetByChangeStrategyAsync(StrategyName);

           foreach (Document document in documents)
           {
               var notification = new IndexingNotification(indexInfo, document.DocumentKey, UmbracoObjectTypes.Document, document.Variations, document.Fields);
               if (await _eventAggregator.PublishCancelableAsync(notification))
               {
                   return;
               }

               await indexInfo.Indexer.AddOrUpdateAsync(indexInfo.IndexAlias, document.DocumentKey, document.ObjectType, document.Variations, notification.Fields, null);
           }
        }
        else
        {
            await RebuildFromMemoryAsync(indexInfo, cancellationToken);
        }
    }

    private async Task<bool> HandleContentChangeAsync(IndexInfo[] indexInfos, ContentChange change, IContentBase content, CancellationToken cancellationToken)
    {
        IndexInfo[] applicableIndexInfos = indexInfos.Where(info => info.ContainedObjectTypes.Contains(change.ObjectType)).ToArray();
        if (applicableIndexInfos.Length is 0)
        {
            return true;
        }

        var result = await _indexingService.IndexContentAsync(indexInfos, content, StrategyName, false, cancellationToken);

        if (change.ChangeImpact is ChangeImpact.RefreshWithDescendants)
        {
            switch (change.ObjectType)
            {
                case UmbracoObjectTypes.Document:
                    await EnumerateDescendantsByPath<IContent>(
                        change.ObjectType,
                        content.Key,
                        (id, pageIndex, pageSize, query, ordering) => _contentService
                            .GetPagedDescendants(id, pageIndex, pageSize, out _, query, ordering)
                            .ToArray(),
                        async descendants =>
                            await HandleDescendantChangesAsync(applicableIndexInfos, descendants, cancellationToken));
                    break;
                case UmbracoObjectTypes.Media:
                    await EnumerateDescendantsByPath<IMedia>(
                        change.ObjectType,
                        content.Key,
                        (id, pageIndex, pageSize, query, ordering) => _mediaService
                            .GetPagedDescendants(id, pageIndex, pageSize, out _, query, ordering)
                            .ToArray(),
                        async descendants =>
                            await HandleDescendantChangesAsync(applicableIndexInfos, descendants, cancellationToken));
                    break;
            }
        }

        return result;
    }

    private async Task HandleDescendantChangesAsync<T>(IndexInfo[] indexInfos, T[] descendants, CancellationToken cancellationToken)
        where T : IContentBase
    {
        foreach (T descendant in descendants)
        {
            await _indexingService.IndexContentAsync(indexInfos, descendant, StrategyName, false, cancellationToken);
        }
    }

    private async Task RemoveFromIndexAsync(IndexInfo[] indexInfos, IReadOnlyCollection<ContentChange> contentChanges)
    {
        if (contentChanges.Count is 0)
        {
            return;
        }

        foreach (IndexInfo indexInfo in indexInfos)
        {
            Guid[] keys = contentChanges
                .Where(change => indexInfo.ContainedObjectTypes.Contains(change.ObjectType))
                .Select(change => change.Id)
                .ToArray();
            await indexInfo.Indexer.DeleteAsync(indexInfo.IndexAlias, keys);

            // Remove from database
            foreach (Guid key in keys)
            {
                await _documentService.DeleteAsync(key, StrategyName);
            }
        }
    }

    private IContentBase? GetContent(ContentChange change)
        => change.ObjectType switch
        {
            UmbracoObjectTypes.Document => _contentService.GetById(change.Id),
            UmbracoObjectTypes.Media => _mediaService.GetById(change.Id),
            UmbracoObjectTypes.Member => _memberService.GetById(change.Id),
            _ => throw new ArgumentOutOfRangeException(nameof(change.ObjectType))
        };

    private async Task RebuildContentFromMemoryAsync(
        IndexInfo indexInfo,
        UmbracoObjectTypes objectType,
        Func<IEnumerable<IContentBase>> getContentAtRoot,
        Func<int, int, IEnumerable<IContentBase>> getPagedContentAtRecycleBinRoot,
        CancellationToken cancellationToken)
    {
        if (indexInfo.ContainedObjectTypes.Contains(objectType) is false)
        {
            return;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            LogIndexRebuildCancellation(indexInfo);
            return;
        }

        foreach (IContentBase rootContent in getContentAtRoot())
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            await _indexingService.IndexContentAsync([indexInfo], rootContent, StrategyName, false, cancellationToken);
            await IndexDescendantsAsync(indexInfo, rootContent, objectType, cancellationToken);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            LogIndexRebuildCancellation(indexInfo);
            return;
        }

        IContentBase[] contentInRecycleBin;
        var pageIndex = 0;
        do
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            contentInRecycleBin = getPagedContentAtRecycleBinRoot(pageIndex, ContentEnumerationPageSize).ToArray();

            foreach (IContentBase content in contentInRecycleBin)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                await _indexingService.IndexContentAsync([indexInfo], content, StrategyName, false, cancellationToken);
            }

            pageIndex++;
        }
        while (contentInRecycleBin.Length == ContentEnumerationPageSize);
    }

    private async Task IndexDescendantsAsync(IndexInfo indexInfo, IContentBase content, UmbracoObjectTypes objectType, CancellationToken cancellationToken)
    {
        switch (objectType)
        {
            case UmbracoObjectTypes.Document:
                await EnumerateDescendantsByPath<IContent>(
                    objectType,
                    content.Key,
                    (id, pageIndex, pageSize, query, ordering) => _contentService
                        .GetPagedDescendants(id, pageIndex, pageSize, out _, query, ordering)
                        .ToArray(),
                    async descendants =>
                    {
                        foreach (IContent descendant in descendants)
                        {
                            await _indexingService.IndexContentAsync([indexInfo], descendant, StrategyName, false, cancellationToken);
                        }
                    });
                break;
            case UmbracoObjectTypes.Media:
                await EnumerateDescendantsByPath<IMedia>(
                    objectType,
                    content.Key,
                    (id, pageIndex, pageSize, query, ordering) => _mediaService
                        .GetPagedDescendants(id, pageIndex, pageSize, out _, query, ordering)
                        .ToArray(),
                    async descendants =>
                    {
                        foreach (IMedia descendant in descendants)
                        {
                            await _indexingService.IndexContentAsync([indexInfo], descendant, StrategyName, false, cancellationToken);
                        }
                    });
                break;
        }
    }

    private async Task RebuildFromMemoryAsync(IndexInfo indexInfo, CancellationToken cancellationToken)
    {
        await RebuildContentFromMemoryAsync(
            indexInfo,
            UmbracoObjectTypes.Document,
            () => _contentService.GetRootContent(),
            (pageIndex, pageSize) => _contentService.GetPagedChildren(Cms.Core.Constants.System.RecycleBinContent, pageIndex, pageSize, out _),
            cancellationToken);

        if (cancellationToken.IsCancellationRequested)
        {
            LogIndexRebuildCancellation(indexInfo);
            return;
        }

        await RebuildContentFromMemoryAsync(
            indexInfo,
            UmbracoObjectTypes.Media,
            () => _mediaService.GetRootMedia(),
            (pageIndex, pageSize) => _mediaService.GetPagedChildren(Cms.Core.Constants.System.RecycleBinMedia, pageIndex, pageSize, out _),
            cancellationToken);

        if (cancellationToken.IsCancellationRequested)
        {
            LogIndexRebuildCancellation(indexInfo);
            return;
        }

        if (indexInfo.ContainedObjectTypes.Contains(UmbracoObjectTypes.Member) is false)
        {
            return;
        }

        IMember[] members;
        var pageIndex = 0;
        do
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            members = _memberService.GetAll(pageIndex, ContentEnumerationPageSize, out _, "sortOrder", Direction.Ascending).ToArray();

            foreach (IMember member in members)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                await _indexingService.IndexContentAsync([indexInfo], member, StrategyName, false, cancellationToken);
            }

            pageIndex++;
        }
        while (members.Length == ContentEnumerationPageSize);

        if (cancellationToken.IsCancellationRequested)
        {
            LogIndexRebuildCancellation(indexInfo);
        }
    }
}
