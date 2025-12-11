using System.Text.Json;
using Microsoft.Extensions.Logging;
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

internal sealed class PublishedContentChangeStrategy : ContentChangeStrategyBase, IPublishedContentChangeStrategy
{
    private readonly IContentProtectionProvider _contentProtectionProvider;
    private readonly IContentService _contentService;
    private readonly IEventAggregator _eventAggregator;
    private readonly IDocumentService _documentService;
    private readonly ILogger<PublishedContentChangeStrategy> _logger;

    protected override bool SupportsTrashedContent => false;

    public PublishedContentChangeStrategy(
        IContentProtectionProvider contentProtectionProvider,
        IContentService contentService,
        IEventAggregator eventAggregator,
        IDocumentService documentService,
        IUmbracoDatabaseFactory umbracoDatabaseFactory,
        IIdKeyMap idKeyMap,
        ILogger<PublishedContentChangeStrategy> logger)
        : base(umbracoDatabaseFactory, idKeyMap, logger)
    {
        _contentProtectionProvider = contentProtectionProvider;
        _contentService = contentService;
        _logger = logger;
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

        foreach (IContent content in _contentService.GetRootContent())
        {
            if (cancellationToken.IsCancellationRequested)
            {
                LogIndexRebuildCancellation(indexInfo);
                return;
            }

            await RebuildContentAsync(indexInfo, content, useDatabase, cancellationToken);
        }
    }

    /// <summary>
    /// Used by HandleAsync: Calculates fields fresh, persists to DB, and adds to index.
    /// </summary>
    private async Task HandleContentChangeAsync(IndexInfo[] indexInfos, IContentBase content, bool forceReindexDescendants, CancellationToken cancellationToken)
    {
        // index the content
        Variation[] indexedVariants = await CalculateAndPersistAsync(indexInfos, content, cancellationToken);
        if (indexedVariants.Any() is false)
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

                    Variation[] indexedVariants = await CalculateAndPersistAsync(indexInfos, descendant, cancellationToken);
                    if (indexedVariants.Any() is false)
                    {
                        // no variants to index, make sure this is removed from the index and skip any descendants moving forward
                        // (the index implementation is responsible for deleting descendants at index level)
                        await RemoveFromIndexAsync(indexInfos, descendant.Key);
                        removedDescendantIds.Add(descendant.Id);
                    }
                }
            });
    }

    /// <summary>
    /// Used by HandleAsync: Always calculates fields fresh, persists to DB, and adds to index.
    /// </summary>
    private async Task<Variation[]> CalculateAndPersistAsync(IndexInfo[] indexInfos, IContentBase content, CancellationToken cancellationToken)
    {
        Variation[] variations = RoutablePublishedVariations(content);
        if (variations.Length is 0)
        {
            return [];
        }

        ContentProtection? contentProtection = await _contentProtectionProvider.GetContentProtectionAsync(content);

        foreach (IndexInfo indexInfo in indexInfos)
        {
            // fetch the doc from service, make sure not to use database here, as it will be deleted
            Document? document = await _documentService.GetAsync(content,  indexInfo.IndexAlias, true, cancellationToken, false);

            if (document is null)
            {
                return [];
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

            await indexInfo.Indexer.AddOrUpdateAsync(indexInfo.IndexAlias, content.Key, UmbracoObjectTypes.Document, variations, notification.Fields, contentProtection);
        }

        return variations;
    }

    /// <summary>
    /// Used by RebuildAsync: Rebuilds content and all descendants from storage.
    /// </summary>
    private async Task RebuildContentAsync(IndexInfo indexInfo, IContentBase content, bool useDatabase, CancellationToken cancellationToken)
    {
        Document? rootDocument = await _documentService.GetAsync(content, indexInfo.IndexAlias, true, cancellationToken, useDatabase);

        // The content was not found, return..
        if (rootDocument is null)
        {
            return;
        }

        await ReIndexDocumentAsync(indexInfo, content, rootDocument, cancellationToken);

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
                // Batch fetch all documents for this page of descendants
                IReadOnlyDictionary<Guid, Document> documents = await _documentService.GetManyAsync(
                    descendants,
                    indexInfo.IndexAlias,
                    true,
                    cancellationToken,
                    useDatabase);

                foreach (IContent descendant in descendants)
                {
                    if (removedDescendantIds.Contains(descendant.ParentId))
                    {
                        continue;
                    }

                    documents.TryGetValue(descendant.Key, out Document? document);
                    if (document is null)
                    {
                        continue;
                    }

                    var indexed = await ReIndexDocumentAsync(indexInfo, descendant, document, cancellationToken);
                    if (indexed is false)
                    {
                        removedDescendantIds.Add(descendant.Id);
                    }
                }
            });
    }

    /// <summary>
    /// Used by RebuildAsync: Uses pre-fetched document if available, falls back to calculating if not found.
    /// </summary>
    private async Task<bool> ReIndexDocumentAsync(IndexInfo indexInfo, IContentBase content, Document document, CancellationToken cancellationToken)
    {
        Variation[] variations = RoutablePublishedVariations(content);
        if (variations.Length is 0)
        {
            return false;
        }

        // the fields collection is for all published variants of the content - but it's not certain that a published
        // variant is also routable, because the published routing state can be broken at ancestor level.
        document.Fields = document.Fields.Where(field => variations.Any(v => (field.Culture is null || v.Culture == field.Culture) && (field.Segment is null || v.Segment == field.Segment))).ToArray();

        ContentProtection? contentProtection = await _contentProtectionProvider.GetContentProtectionAsync(content);

        await _documentService.DeleteAsync(content.Key, indexInfo.IndexAlias);
        await _documentService.AddAsync(document);

        var notification = new IndexingNotification(indexInfo, content.Key, UmbracoObjectTypes.Document, variations, document.Fields);
        if (await _eventAggregator.PublishCancelableAsync(notification))
        {
            return true;
        }


        await indexInfo.Indexer.AddOrUpdateAsync(indexInfo.IndexAlias, content.Key, UmbracoObjectTypes.Document, variations, notification.Fields, contentProtection);

        return true;
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
                await _documentService.DeleteAsync(id, indexInfo.IndexAlias);
            }
        }
    }

    // NOTE: for the time being, segments are not individually publishable, but it will likely happen at some point,
    //       so this method deals with variations - not cultures.
    private Variation[] RoutablePublishedVariations(IContentBase content)
    {
        if (content.IsPublished() is false)
        {
            return [];
        }

        var variesByCulture = content.VariesByCulture();

        // if the content varies by culture, the indexable cultures are the published
        // cultures - otherwise "null" represents "no culture"
        var cultures = content.PublishedCultures();

        // now iterate all ancestors and make sure all cultures are published all the way up the tree
        foreach (var ancestorId in content.AncestorIds())
        {
            IContent? ancestor = _contentService.GetById(ancestorId);
            if (ancestor is null || ancestor.Published is false)
            {
                // no published ancestor => don't index anything
                cultures = [];
            }
            else if (variesByCulture && ancestor.VariesByCulture())
            {
                // both the content and the ancestor are culture variant => only index the published cultures they have in common
                cultures = cultures.Intersect(ancestor.PublishedCultures).ToArray();
            }

            // if we've already run out of cultures to index, there is no reason to iterate the ancestors any further
            if (cultures.Any() == false)
            {
                break;
            }
        }

        // for now, segments are not individually routable, so we only need to deal with cultures and append all known segments
        if (content.Properties.Any(p => p.PropertyType.VariesBySegment()) is false)
        {
            // no segment variant properties - just return the found cultures
            return cultures.Select(c => new Variation(c, null)).ToArray();
        }

        // segments are not "known" - we can only determine segment variation by looking at the property values
        return cultures.SelectMany(culture => content
                .Properties
                .SelectMany(property => property.Values.Where(value => value.Culture.InvariantEquals(culture)))
                .DistinctBy(value => value.Segment).Select(value => value.Segment)
                .Select(segment => new Variation(culture, segment)))
            .ToArray();
    }
}
