using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Search.Core.Extensions;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Models.Persistence;
using Umbraco.Cms.Search.Core.Notifications;
using Umbraco.Extensions;

namespace Umbraco.Cms.Search.Core.Services.ContentIndexing;

internal sealed class DraftContentChangeStrategy : ContentChangeStrategyBase, IDraftContentChangeStrategy
{
    private readonly IContentService _contentService;
    private readonly IMediaService _mediaService;
    private readonly IMemberService _memberService;
    private readonly IEventAggregator _eventAggregator;
    private readonly IDocumentService _documentService;
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
        ILogger<DraftContentChangeStrategy> logger)
        : base(umbracoDatabaseFactory, idKeyMap, logger)
    {
        _contentService = contentService;
        _mediaService = mediaService;
        _memberService = memberService;
        _eventAggregator = eventAggregator;
        _documentService = documentService;
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

        var result = await CalculateAndPersistAsync(applicableIndexInfos, content, change.ObjectType, cancellationToken);

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
                            await HandleDescendantChangesAsync(applicableIndexInfos, descendants, change.ObjectType, cancellationToken));
                    break;
                case UmbracoObjectTypes.Media:
                    await EnumerateDescendantsByPath<IMedia>(
                        change.ObjectType,
                        content.Key,
                        (id, pageIndex, pageSize, query, ordering) => _mediaService
                            .GetPagedDescendants(id, pageIndex, pageSize, out _, query, ordering)
                            .ToArray(),
                        async descendants =>
                            await HandleDescendantChangesAsync(applicableIndexInfos, descendants, change.ObjectType, cancellationToken));
                    break;
            }
        }

        return result;
    }

    private async Task HandleDescendantChangesAsync<T>(IndexInfo[] indexInfos, T[] descendants, UmbracoObjectTypes objectType, CancellationToken cancellationToken)
        where T : IContentBase
    {
        foreach (T descendant in descendants)
        {
            await CalculateAndPersistAsync(indexInfos, descendant, objectType, cancellationToken);
        }
    }

    /// <summary>
    /// Calculates fields from content, persists to DB, and adds to index.
    /// </summary>
    private async Task<bool> CalculateAndPersistAsync(IndexInfo[] indexInfos, IContentBase content, UmbracoObjectTypes objectType, CancellationToken cancellationToken)
    {
        // fetch the doc from service, make sure not to use database here, as it will be deleted
        Document? document = await _documentService.CalculateDocument(content, false, cancellationToken);

        if (document is null)
        {
            return false;
        }

        // Delete old entry and persist new fields to database
        await _documentService.DeleteAsync(content.Key, StrategyName);
        await _documentService.AddAsync(document, StrategyName);

        foreach (IndexInfo indexInfo in indexInfos)
        {
            var notification = new IndexingNotification(indexInfo, content.Key, UmbracoObjectTypes.Document, document.Variations, document.Fields);
            if (await _eventAggregator.PublishCancelableAsync(notification))
            {
                // the indexing operation was cancelled for this index; continue with the rest of the indexes
                continue;
            }

            await indexInfo.Indexer.AddOrUpdateAsync(indexInfo.IndexAlias, content.Key, objectType, document.Variations, notification.Fields, null);
        }

        return true;
    }

    private async Task IndexDocumentAsync(IndexInfo indexInfo, Document document, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            LogIndexRebuildCancellation(indexInfo);
        }

        // Add to database and add all fields, these might be filtered later by end user.
        await _documentService.AddAsync(document, StrategyName);

        var notification = new IndexingNotification(indexInfo, document.DocumentKey, UmbracoObjectTypes.Document, document.Variations, document.Fields);
        if (await _eventAggregator.PublishCancelableAsync(notification))
        {
            return;
        }

        // Add to index
        await indexInfo.Indexer.AddOrUpdateAsync(indexInfo.IndexAlias, document.DocumentKey, document.ObjectType, document.Variations, notification.Fields, null);
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

            await CalculateAndPersistAsync([indexInfo], rootContent, objectType, cancellationToken);
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

                await CalculateAndPersistAsync([indexInfo], content, objectType, cancellationToken);
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
                            Document? document = await _documentService.CalculateDocument(descendant, false, cancellationToken);
                            if (document is null)
                            {
                                continue;
                            }

                            await IndexDocumentAsync(indexInfo, document, cancellationToken);
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
                            Document? document = await _documentService.CalculateDocument(descendant, false, cancellationToken);
                            if (document is null)
                            {
                                continue;
                            }

                            await IndexDocumentAsync(indexInfo, document, cancellationToken);
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

                Document? document = await _documentService.CalculateDocument(member, false, cancellationToken);
                if (document is null)
                {
                    continue;
                }

                await IndexDocumentAsync(indexInfo, document, cancellationToken);
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
