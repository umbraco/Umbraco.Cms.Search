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

        await RebuildContentAsync(
            indexInfo,
            UmbracoObjectTypes.Document,
            () => _contentService.GetRootContent(),
            (pageIndex, pageSize) => _contentService.GetPagedChildren(Cms.Core.Constants.System.RecycleBinContent, pageIndex, pageSize, out _),
            useDatabase,
            cancellationToken);

        if (cancellationToken.IsCancellationRequested)
        {
            LogIndexRebuildCancellation(indexInfo);
            return;
        }

        await RebuildContentAsync(
            indexInfo,
            UmbracoObjectTypes.Media,
            () => _mediaService.GetRootMedia(),
            (pageIndex, pageSize) => _mediaService.GetPagedChildren(Cms.Core.Constants.System.RecycleBinMedia, pageIndex, pageSize, out _),
            useDatabase,
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

            // Batch fetch all documents for this page of members
            IReadOnlyDictionary<Guid, Document> documents = await _documentService.GetManyAsync(
                members,
                indexInfo.IndexAlias,
                false,
                cancellationToken,
                useDatabase);

            foreach (IMember member in members)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                documents.TryGetValue(member.Key, out Document? document);
                if (document is null)
                {
                    continue;
                }

                await ReIndexDocumentAsync(indexInfo, member, UmbracoObjectTypes.Member, document, cancellationToken);
            }

            pageIndex++;
        }
        while (members.Length == ContentEnumerationPageSize);

        if (cancellationToken.IsCancellationRequested)
        {
            LogIndexRebuildCancellation(indexInfo);
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
        Variation[] variations = GetVariations(content);

        foreach (IndexInfo indexInfo in indexInfos)
        {
            // fetch the doc from service, make sure not to use database here, as it will be deleted
            Document? document = await _documentService.GetAsync(content, indexInfo.IndexAlias, false, cancellationToken, false);

            if (document is null)
            {
                return false;
            }

            // Delete old entry and persist new fields to database
            await _documentService.DeleteAsync(content.Key, indexInfo.IndexAlias);
            await _documentService.AddAsync(document);

            var notification = new IndexingNotification(indexInfo, content.Key, UmbracoObjectTypes.Document, variations, document.Fields);
            if (await _eventAggregator.PublishCancelableAsync(notification))
            {
                // the indexing operation was cancelled for this index; continue with the rest of the indexes
                continue;
            }

            await indexInfo.Indexer.AddOrUpdateAsync(indexInfo.IndexAlias, content.Key, objectType, variations, notification.Fields, null);
        }

        return true;
    }

    /// <summary>
    /// Used by RebuildAsync: Uses pre-fetched document to reindex content.
    /// </summary>
    private async Task ReIndexDocumentAsync(IndexInfo indexInfo, IContentBase content, UmbracoObjectTypes objectType, Document document, CancellationToken cancellationToken)
    {
        Variation[] variations = GetVariations(content);

        await _documentService.DeleteAsync(content.Key, indexInfo.IndexAlias);
        await _documentService.AddAsync(document);

        var notification = new IndexingNotification(indexInfo, content.Key, UmbracoObjectTypes.Document, variations, document.Fields);
        if (await _eventAggregator.PublishCancelableAsync(notification))
        {
            return;
        }

        await indexInfo.Indexer.AddOrUpdateAsync(indexInfo.IndexAlias, content.Key, objectType, variations, notification.Fields, null);
    }

    private Variation[] GetVariations(IContentBase content)
    {
        string?[] cultures = content.AvailableCultures();

        return content.ContentType.VariesBySegment()
            ? cultures
                .SelectMany(culture => content
                    .Properties
                    .SelectMany(property => property.Values.Where(value => value.Culture.InvariantEquals(culture)))
                    .DistinctBy(value => value.Segment).Select(value => value.Segment)
                    .Select(segment => new Variation(culture, segment)))
                .ToArray()
            : cultures
                .Select(culture => new Variation(culture, null))
                .ToArray();
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
                await _documentService.DeleteAsync(key, indexInfo.IndexAlias);
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

    private async Task RebuildContentAsync(
        IndexInfo indexInfo,
        UmbracoObjectTypes objectType,
        Func<IEnumerable<IContentBase>> getContentAtRoot,
        Func<int, int, IEnumerable<IContentBase>> getPagedContentAtRecycleBinRoot,
        bool useDatabase,
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

            Document? rootDocument = await _documentService.GetAsync(rootContent, indexInfo.IndexAlias, false, cancellationToken, useDatabase);

            if (rootDocument is null)
            {
                continue;
            }

            await ReIndexDocumentAsync(indexInfo, rootContent, objectType, rootDocument, cancellationToken);
            await RebuildDescendantsAsync(indexInfo, rootContent, objectType, useDatabase, cancellationToken);
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

            // Batch fetch all documents for this page of recycle bin content
            IReadOnlyDictionary<Guid, Document> documents = await _documentService.GetManyAsync(
                contentInRecycleBin,
                indexInfo.IndexAlias,
                false,
                cancellationToken,
                useDatabase);

            foreach (IContentBase content in contentInRecycleBin)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                documents.TryGetValue(content.Key, out Document? document);
                if (document is null)
                {
                    continue;
                }

                await ReIndexDocumentAsync(indexInfo, content, objectType, document, cancellationToken);
                await RebuildDescendantsAsync(indexInfo, content, objectType, useDatabase, cancellationToken);
            }

            pageIndex++;
        }
        while (contentInRecycleBin.Length == ContentEnumerationPageSize);
    }

    private async Task RebuildDescendantsAsync(IndexInfo indexInfo, IContentBase content, UmbracoObjectTypes objectType, bool useDatabase, CancellationToken cancellationToken)
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
                        IReadOnlyDictionary<Guid, Document> documents = await _documentService.GetManyAsync(
                            descendants,
                            indexInfo.IndexAlias,
                            false,
                            cancellationToken,
                            useDatabase);

                        foreach (IContent descendant in descendants)
                        {
                            documents.TryGetValue(descendant.Key, out Document? document);
                            if (document is null)
                            {
                                continue;
                            }

                            await ReIndexDocumentAsync(indexInfo, descendant, objectType, document, cancellationToken);
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
                        IReadOnlyDictionary<Guid, Document> documents = await _documentService.GetManyAsync(
                            descendants,
                            indexInfo.IndexAlias,
                            false,
                            cancellationToken,
                            useDatabase);

                        foreach (IMedia descendant in descendants)
                        {
                            documents.TryGetValue(descendant.Key, out Document? document);
                            if (document is null)
                            {
                                continue;
                            }

                            await ReIndexDocumentAsync(indexInfo, descendant, objectType, document, cancellationToken);
                        }
                    });
                break;
        }
    }
}
