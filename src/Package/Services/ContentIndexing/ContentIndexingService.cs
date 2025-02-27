using MessagePack;
using Microsoft.Extensions.Logging;
using Package.Models.Indexing;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Changes;
using Umbraco.Cms.Infrastructure.HostedServices;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Extensions;

namespace Package.Services.ContentIndexing;

internal sealed class ContentIndexingService : IContentIndexingService
{
    private readonly IIndexService _indexService;
    private readonly IContentIndexingDataCollectionService _contentIndexingDataCollectionService;
    private readonly IContentService _contentService;
    private readonly IUmbracoDatabaseFactory _umbracoDatabaseFactory;
    private readonly IIdKeyMap _idKeyMap;
    private readonly IBackgroundTaskQueue _backgroundTaskQueue;
    private readonly ILogger<ContentIndexingService> _logger;

    public ContentIndexingService(
        IIndexService indexService,
        IContentIndexingDataCollectionService contentIndexingDataCollectionService,
        IContentService contentService,
        IUmbracoDatabaseFactory umbracoDatabaseFactory,
        IIdKeyMap idKeyMap,
        IBackgroundTaskQueue backgroundTaskQueue,
        ILogger<ContentIndexingService> logger)
    {
        _indexService = indexService;
        _contentService = contentService;
        _umbracoDatabaseFactory = umbracoDatabaseFactory;
        _idKeyMap = idKeyMap;
        _backgroundTaskQueue = backgroundTaskQueue;
        _logger = logger;
        _contentIndexingDataCollectionService = contentIndexingDataCollectionService;
    }

    public void Handle(IEnumerable<ContentChange> changes)
        => _backgroundTaskQueue.QueueBackgroundWorkItem(async cancellationToken => await HandleAsync(changes, cancellationToken));

    // internal for testability
    // TODO: support protected content
    internal async Task HandleAsync(IEnumerable<ContentChange> changes, CancellationToken cancellationToken)
    {
        var pendingRemovals = new List<Guid>();
        foreach (var change in changes)
        {
            var remove = change.ChangeTypes.HasType(TreeChangeTypes.Remove);
            var reindex = change.ChangeTypes.HasType(TreeChangeTypes.RefreshNode)
                          || change.ChangeTypes.HasType(TreeChangeTypes.RefreshBranch);

            if (remove)
            {
                pendingRemovals.Add(change.Id);
            }
            else if (reindex)
            {
                var content = _contentService.GetById(change.Id);
                if (content == null || content.Trashed)
                {
                    pendingRemovals.Add(change.Id);
                    continue;
                }

                RemoveFromIndex(pendingRemovals);
                pendingRemovals.Clear();

                await ReindexAsync(content, cancellationToken);
            }
        }

        RemoveFromIndex(pendingRemovals);
    }
    
    private async Task ReindexAsync(IContent content, CancellationToken cancellationToken)
    {
        // get the currently indexed variants for the content
        var currentVariants = await CurrentVariantsAsync(content, cancellationToken);

        // index the content
        var indexedVariants = await UpdateIndexAsync(content, cancellationToken);
        if (indexedVariants.Any() is false)
        {
            // we likely got here because a removal triggered a "refresh branch" notification, now we
            // need to delete every last culture of this content and all descendants
            RemoveFromIndex(content.Key);
            return;
        }

        // if the published state changed of any variant, chances are there are similar changes ot the content descendants
        // that need to be reflected in the index, so we'll reindex all descendants
        var anyVariantsChanged = indexedVariants.Length != currentVariants.Length || indexedVariants.Except(currentVariants).Any();
        if (anyVariantsChanged)
        {
            await ReindexDescendantsAsync(content, cancellationToken);
        }
    }

    private async Task ReindexDescendantsAsync(IContent content, CancellationToken cancellationToken)
    {
        var removedDescendantIds = new List<int>();
        await EnumerateDescendantsByPath(content.Key, async descendants =>
        {
            // NOTE: this works because we're enumerating descendants by path
            foreach (IContent descendant in descendants)
            {
                if (removedDescendantIds.Contains(descendant.ParentId))
                {
                    continue;
                }

                var indexedVariants = await UpdateIndexAsync(descendant, cancellationToken);
                if (indexedVariants.Any() is false)
                {
                    // no variants to index, make sure this is removed from the index and skip any descendants moving forward
                    // (the index implementation is responsible for deleting descendants at index level)
                    RemoveFromIndex(descendant.Key);
                    removedDescendantIds.Add(descendant.Id);
                }
            }
        });
    }

    private async Task<ContentMetadataVariation[]> UpdateIndexAsync(IContent content, CancellationToken cancellationToken)
    {
        var variations = RoutablePublishedVariations(content);
        if (variations.Length is 0)
        {
            return [];
        }

        var fields = await _contentIndexingDataCollectionService.CollectAsync(content, true, cancellationToken);
        if (fields is null)
        {
            return [];
        }

        // the fields collection is for all published variants of the content - but it's not certain that a published
        // variant is also routable, because the published routing state can be broken at ancestor level.
        fields = fields.Where(field => variations.Any(v => (field.Culture is null || v.Culture == field.Culture) && (field.Segment is null || v.Segment == field.Segment)));

        var metadata = new ContentMetadata
        {
            Variations = variations
                .Select(v => new ContentMetadataVariation { Culture = v.Culture, Segment = v.Segment})
                .ToArray()
        };

        var metadataBytes = MessagePackSerializer.Serialize(metadata, cancellationToken: cancellationToken);
        var metadataStamp = Convert.ToBase64String(metadataBytes);

        await _indexService.AddOrUpdateAsync(content.Key, metadataStamp, variations, fields);

        return metadata.Variations;
    }

    private async Task<ContentMetadataVariation[]> CurrentVariantsAsync(IContent content, CancellationToken cancellationToken)
    {
        var metadataStamp = await _indexService.GetStampAsync(content.Key);
        if (metadataStamp is null)
        {
            return [];
        }

        var metadataBytes = Convert.FromBase64String(metadataStamp);
        var metadata = MessagePackSerializer.Deserialize<ContentMetadata>(metadataBytes, cancellationToken: cancellationToken);
        return metadata.Variations;
    }

    private void RemoveFromIndex(Guid id)
        => RemoveFromIndex([id]);

    private void RemoveFromIndex(IReadOnlyCollection<Guid> ids)
    {
        _indexService.DeleteAsync(ids);
    }

    private async Task EnumerateDescendantsByPath(Guid rootId, Func<IContent[], Task> actionToPerform)
    {
        var rootIntIdAttempt = _idKeyMap.GetIdForKey(rootId, UmbracoObjectTypes.Document);
        if (rootIntIdAttempt.Success is false)
        {
            _logger.LogWarning("Could not resolve ID for content item {rootId} - aborting enumerations of descendants.", rootId);
            return;
        }

        const int pageSize = 10000;
        var pageIndex = 0;

        IContent[] descendants;
        var query = _umbracoDatabaseFactory.SqlContext.Query<IContent>().Where(content => content.Trashed == false);
        do
        {
            descendants = _contentService
                .GetPagedDescendants(rootIntIdAttempt.Result, pageIndex, pageSize, out _, query, Ordering.By("Path"))
                .ToArray();

            await actionToPerform(descendants.ToArray());

            pageIndex++;
        } while (descendants.Length == pageSize);
    }

    // NOTE: for the time being, segments are not individually publishable, but it will likely happen at some point,
    //       so this method deals with variations - not cultures.
    private Variation[] RoutablePublishedVariations(IContent content)
    {
        if (content.Published == false)
        {
            return [];
        }

        var variesByCulture = content.ContentType.VariesByCulture();

        // if the content varies by culture, the indexable cultures are the published
        // cultures - otherwise "null" represents "no culture"
        var cultures = variesByCulture
            ? content.PublishedCultures.ToArray()
            : new string?[] { null };

        // now iterate all ancestors and make sure all cultures are published all the way up the tree
        foreach (var ancestorId in content.GetAncestorIds() ?? [])
        {
            IContent? ancestor = _contentService.GetById(ancestorId);
            if (ancestor is null || ancestor.Published is false)
            {
                // no published ancestor => don't index anything
                cultures = [];
            }
            else if (variesByCulture && ancestor.ContentType.VariesByCulture())
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
            .SelectMany(property =>
                property.Values.Where(value => value.Culture.InvariantEquals(culture))
            )
            .DistinctBy(value => value.Segment).Select(value => value.Segment)
            .Select(segment => new Variation(culture, segment))
        ).ToArray();
    }
}