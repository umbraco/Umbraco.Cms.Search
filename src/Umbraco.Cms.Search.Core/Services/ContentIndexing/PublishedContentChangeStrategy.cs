using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Search.Core.Extensions;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Extensions;

namespace Umbraco.Cms.Search.Core.Services.ContentIndexing;

internal class PublishedContentChangeStrategy : IPublishedContentChangeStrategy
{
    private readonly IContentIndexingDataCollectionService _contentIndexingDataCollectionService;
    private readonly IContentProtectionProvider _contentProtectionProvider;
    private readonly IContentService _contentService;
    private readonly IUmbracoDatabaseFactory _umbracoDatabaseFactory;
    private readonly IIdKeyMap _idKeyMap;
    private readonly ILogger<PublishedContentChangeStrategy> _logger;

    public PublishedContentChangeStrategy(
        IContentIndexingDataCollectionService contentIndexingDataCollectionService,
        IContentProtectionProvider contentProtectionProvider,
        IContentService contentService,
        IUmbracoDatabaseFactory umbracoDatabaseFactory,
        IIdKeyMap idKeyMap,
        ILogger<PublishedContentChangeStrategy> logger)
    {
        _contentIndexingDataCollectionService = contentIndexingDataCollectionService;
        _contentProtectionProvider = contentProtectionProvider;
        _contentService = contentService;
        _umbracoDatabaseFactory = umbracoDatabaseFactory;
        _idKeyMap = idKeyMap;
        _logger = logger;
    }

    public async Task HandleAsync(IEnumerable<IndexInfo> indexInfos, IEnumerable<ContentChange> changes, CancellationToken cancellationToken)
    {
        // make sure all indexes can handle documents
        var indexInfosAsArray = indexInfos as IndexInfo[] ?? indexInfos.ToArray();
        if (indexInfosAsArray.Any(indexInfo => indexInfo.ContainedObjectTypes.Contains(UmbracoObjectTypes.Document) is false))
        {
            _logger.LogWarning("One or more indexes for unsupported object types were detected and skipped. This strategy only supports Documents.");
            indexInfosAsArray = indexInfosAsArray.Where(indexInfo => indexInfo.ContainedObjectTypes.Contains(UmbracoObjectTypes.Document)).ToArray();
        }

        // get the relevant changes for this change strategy
        var changesAsArray = changes.Where(change =>
            change.ContentState is ContentState.Published
            && change.ObjectType is UmbracoObjectTypes.Document
        ).ToArray();

        var pendingRemovals = new List<Guid>();
        foreach (var change in changesAsArray.Where(change => change.ContentState is ContentState.Published))
        {
            if (change.ChangeImpact is ChangeImpact.Remove)
            {
                pendingRemovals.Add(change.Id);
            }
            else
            {
                var content = _contentService.GetById(change.Id);
                if (content is null || content.Trashed)
                {
                    pendingRemovals.Add(change.Id);
                    continue;
                }

                await RemoveFromIndexAsync(indexInfosAsArray, pendingRemovals);
                pendingRemovals.Clear();

                await ReindexAsync(indexInfosAsArray, content, change.ChangeImpact is ChangeImpact.RefreshWithDescendants, cancellationToken);
            }
        }

        await RemoveFromIndexAsync(indexInfosAsArray, pendingRemovals);
    }

    private async Task ReindexAsync(IndexInfo[] indexInfos, IContentBase content, bool forceReindexDescendants, CancellationToken cancellationToken)
    {
        // index the content
        var indexedVariants = await UpdateIndexAsync(indexInfos, content, cancellationToken);
        if (indexedVariants.Any() is false)
        {
            // we likely got here because a removal triggered a "refresh branch" notification, now we
            // need to delete every last culture of this content and all descendants
            await RemoveFromIndexAsync(indexInfos, content.Key);
            return;
        }

        if (forceReindexDescendants)
        {
            await ReindexDescendantsAsync(indexInfos, content, cancellationToken);
        }
    }

    private async Task ReindexDescendantsAsync(IndexInfo[] indexInfos, IContentBase content, CancellationToken cancellationToken)
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

                var indexedVariants = await UpdateIndexAsync(indexInfos, descendant, cancellationToken);
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

    private async Task<Variation[]> UpdateIndexAsync(IndexInfo[] indexInfos, IContentBase content, CancellationToken cancellationToken)
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
        fields = fields.Where(field => variations.Any(v => (field.Culture is null || v.Culture == field.Culture) && (field.Segment is null || v.Segment == field.Segment))).ToArray();

        var contentProtection = await _contentProtectionProvider.GetContentProtectionAsync(content);

        foreach (var indexInfo in indexInfos)
        {
            await indexInfo.IndexService.AddOrUpdateAsync(indexInfo.IndexAlias, content.Key, UmbracoObjectTypes.Document, variations, fields, contentProtection);
        }

        return variations;
    }

    private async Task RemoveFromIndexAsync(IndexInfo[] indexInfos, Guid id)
        => await RemoveFromIndexAsync(indexInfos, [id]);

    private async Task RemoveFromIndexAsync(IndexInfo[] indexInfos, IReadOnlyCollection<Guid> ids)
    {
        if (ids.Count is 0)
        {
            return;
        }

        foreach (var indexInfo in indexInfos)
        {
            await indexInfo.IndexService.DeleteAsync(indexInfo.IndexAlias, ids);
        }
    }

    private async Task EnumerateDescendantsByPath(Guid rootId, Func<IContent[], Task> actionToPerform)
    {
        var rootIdAttempt = _idKeyMap.GetIdForKey(rootId, UmbracoObjectTypes.Document);
        if (rootIdAttempt.Success is false)
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
                .GetPagedDescendants(rootIdAttempt.Result, pageIndex, pageSize, out _, query, Ordering.By("Path"))
                .ToArray();

            await actionToPerform(descendants.ToArray());

            pageIndex++;
        } while (descendants.Length == pageSize);
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
            .SelectMany(property =>
                property.Values.Where(value => value.Culture.InvariantEquals(culture))
            )
            .DistinctBy(value => value.Segment).Select(value => value.Segment)
            .Select(segment => new Variation(culture, segment))
        ).ToArray();
    }
}