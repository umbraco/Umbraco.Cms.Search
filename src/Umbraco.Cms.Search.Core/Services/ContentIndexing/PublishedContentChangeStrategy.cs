using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Models.Persistence;
using Umbraco.Cms.Search.Core.Notifications;

namespace Umbraco.Cms.Search.Core.Services.ContentIndexing;

internal sealed class PublishedContentChangeStrategy : ContentChangeStrategyBase, IPublishedContentChangeStrategy
{
    private readonly IContentService _contentService;
    private readonly IEventAggregator _eventAggregator;
    private readonly IDocumentService _documentService;
    private readonly ILogger<PublishedContentChangeStrategy> _logger;
    private readonly IIndexingService _indexingService;
    private const string StrategyName = "PublishedContentChangeStrategy";

    protected override bool SupportsTrashedContent => false;

    public PublishedContentChangeStrategy(
        IContentService contentService,
        IEventAggregator eventAggregator,
        IDocumentService documentService,
        IUmbracoDatabaseFactory umbracoDatabaseFactory,
        IIdKeyMap idKeyMap,
        ILogger<PublishedContentChangeStrategy> logger,
        IIndexingService indexingService)
        : base(umbracoDatabaseFactory, idKeyMap, logger)
    {
        _contentService = contentService;
        _logger = logger;
        _indexingService = indexingService;
        _eventAggregator = eventAggregator;
        _documentService = documentService;
    }

    public async Task HandleAsync(IEnumerable<IndexInfo> indexInfos, IEnumerable<ContentChange> changes, CancellationToken cancellationToken)
    {
        // make sure all indexes can handle documents
        IndexInfo[] indexInfosAsArray = indexInfos as IndexInfo[] ?? indexInfos.ToArray();
        if (indexInfosAsArray.Any(indexInfo => indexInfo.ContainedObjectTypes.Contains(UmbracoObjectTypes.Document) is false))
        {
            _logger.LogWarning("One or more indexes for unsupported object types were detected and skipped. This strategy only supports Documents.");
            indexInfosAsArray = indexInfosAsArray.Where(indexInfo => indexInfo.ContainedObjectTypes.Contains(UmbracoObjectTypes.Document)).ToArray();
        }

        // get the relevant changes for this change strategy
        ContentChange[] changesAsArray = changes.Where(change =>
                change.ContentState is ContentState.Published
                && change.ObjectType is UmbracoObjectTypes.Document)
            .ToArray();

        var pendingRemovals = new List<Guid>();
        foreach (ContentChange change in changesAsArray)
        {
            if (change.ChangeImpact is ChangeImpact.Remove)
            {
                pendingRemovals.Add(change.Id);
            }
            else
            {
                IContent? content = _contentService.GetById(change.Id);
                if (content is null || content.Trashed)
                {
                    pendingRemovals.Add(change.Id);
                    continue;
                }

                await RemoveFromIndexAsync(indexInfosAsArray, pendingRemovals);
                pendingRemovals.Clear();

                await HandleContentChangeAsync(indexInfosAsArray, content, change.ChangeImpact is ChangeImpact.RefreshWithDescendants, cancellationToken);
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


                await indexInfo.Indexer.AddOrUpdateAsync(indexInfo.IndexAlias, document.DocumentKey, UmbracoObjectTypes.Document, document.Variations, notification.Fields, document.Protection);
            }
        }
        else
        {
            await RebuildFromMemoryAsync(indexInfo, cancellationToken);
        }
    }

    private async Task RebuildFromMemoryAsync(IndexInfo indexInfo, CancellationToken cancellationToken)
    {
        foreach (IContent content in _contentService.GetRootContent())
        {
            if (cancellationToken.IsCancellationRequested)
            {
                LogIndexRebuildCancellation(indexInfo);
                return;
            }

            await RebuildContentFromMemoryAsync(indexInfo, content, cancellationToken);
        }
    }

    /// <summary>
    /// Used by HandleAsync: Calculates fields fresh, persists to DB, and adds to index.
    /// </summary>
    private async Task HandleContentChangeAsync(IndexInfo[] indexInfos, IContentBase content, bool forceReindexDescendants, CancellationToken cancellationToken)
    {
        // index the content
        var result = await _indexingService.IndexContentAsync(indexInfos, content, StrategyName, true, cancellationToken);
        if (result is false)
        {
            // we likely got here because a removal triggered a "refresh branch" notification, now we
            // need to delete every last culture of this content and all descendants
            await RemoveFromIndexAsync(indexInfos, content.Key);
            return;
        }

        if (forceReindexDescendants)
        {
            await HandleDescendantChangesAsync(indexInfos, content, cancellationToken);
        }
    }

    private async Task HandleDescendantChangesAsync(IndexInfo[] indexInfos, IContentBase content, CancellationToken cancellationToken)
    {
        var removedDescendantIds = new List<int>();
        await EnumerateDescendantsByPath<IContent>(
            UmbracoObjectTypes.Document,
            content.Key,
            (id, pageIndex, pageSize, query, ordering) => _contentService
                .GetPagedDescendants(id, pageIndex, pageSize, out _, query, ordering)
                .ToArray(),
            async descendants =>
            {
                // NOTE: this works because we're enumerating descendants by path
                foreach (IContent descendant in descendants)
                {
                    if (removedDescendantIds.Contains(descendant.ParentId))
                    {
                        continue;
                    }

                    var result = await _indexingService.IndexContentAsync(indexInfos, descendant, StrategyName, true, cancellationToken);
                    if (result is false)
                    {
                        // no variants to index, make sure this is removed from the index and skip any descendants moving forward
                        // (the index implementation is responsible for deleting descendants at index level)
                        await RemoveFromIndexAsync(indexInfos, descendant.Key);
                        removedDescendantIds.Add(descendant.Id);
                    }
                }
            });
    }

    private async Task RebuildContentFromMemoryAsync(IndexInfo indexInfo, IContentBase content, CancellationToken cancellationToken)
    {
        var result = await _indexingService.IndexContentAsync([indexInfo], content, StrategyName, true, cancellationToken);

        if (result is false)
        {
            return;
        }

        // Rebuild all descendants
        var removedDescendantIds = new List<int>();
        await EnumerateDescendantsByPath<IContent>(
            UmbracoObjectTypes.Document,
            content.Key,
            (id, pageIndex, pageSize, query, ordering) => _contentService
                .GetPagedDescendants(id, pageIndex, pageSize, out _, query, ordering)
                .ToArray(),
            async descendants =>
            {
                foreach (IContent descendant in descendants)
                {
                    if (removedDescendantIds.Contains(descendant.ParentId))
                    {
                        continue;
                    }

                    var descendantResult = await _indexingService.IndexContentAsync([indexInfo], content, StrategyName, true, cancellationToken);

                    if (descendantResult is false)
                    {
                        removedDescendantIds.Add(descendant.Id);
                    }
                }
            });
    }

    private async Task RemoveFromIndexAsync(IndexInfo[] indexInfos, Guid id)
        => await RemoveFromIndexAsync(indexInfos, [id]);

    private async Task RemoveFromIndexAsync(IndexInfo[] indexInfos, IReadOnlyCollection<Guid> ids)
    {
        if (ids.Count is 0)
        {
            return;
        }

        foreach (IndexInfo indexInfo in indexInfos)
        {
            await indexInfo.Indexer.DeleteAsync(indexInfo.IndexAlias, ids);

            // Remove from database
            foreach (Guid id in ids)
            {
                await _documentService.DeleteAsync(id, StrategyName);
            }
        }
    }
}
