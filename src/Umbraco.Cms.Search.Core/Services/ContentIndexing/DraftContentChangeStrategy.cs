using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Search.Core.Extensions;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Extensions;

namespace Umbraco.Cms.Search.Core.Services.ContentIndexing;

internal class DraftContentChangeStrategy : ContentChangeStrategyBase, IDraftContentChangeStrategy
{
    private readonly IContentIndexingDataCollectionService _contentIndexingDataCollectionService;
    private readonly IContentService _contentService;
    private readonly IMediaService _mediaService;
    private readonly IMemberService _memberService;

    protected override bool SupportsTrashedContent => true;

    public DraftContentChangeStrategy(
        IContentIndexingDataCollectionService contentIndexingDataCollectionService,
        IContentService contentService,
        IMediaService mediaService,
        IMemberService memberService,
        IUmbracoDatabaseFactory umbracoDatabaseFactory,
        IIdKeyMap idKeyMap,
        ILogger<DraftContentChangeStrategy> logger)
        : base(umbracoDatabaseFactory, idKeyMap, logger)
    {
        _contentIndexingDataCollectionService = contentIndexingDataCollectionService;
        _contentService = contentService;
        _mediaService = mediaService;
        _memberService = memberService;
    }

    public async Task HandleAsync(IEnumerable<IndexInfo> indexInfos, IEnumerable<ContentChange> changes, CancellationToken cancellationToken)
    {
        var indexInfosAsArray = indexInfos as IndexInfo[] ?? indexInfos.ToArray();

        // get the relevant changes for this change strategy
        var changesAsArray = changes.Where(change =>
            change.ContentState is ContentState.Draft
            && change.ObjectType is UmbracoObjectTypes.Document or UmbracoObjectTypes.Media or UmbracoObjectTypes.Member
        ).ToArray();

        var pendingRemovals = new List<ContentChange>();
        foreach (var change in changesAsArray.Where(change => change.ContentState is ContentState.Draft))
        {
            if (change.ChangeImpact is ChangeImpact.Remove)
            {
                pendingRemovals.Add(change);
            }
            else
            {
                var content = GetContent(change);
                if (content is null)
                {
                    pendingRemovals.Add(change);
                    continue;
                }

                await RemoveFromIndexAsync(indexInfosAsArray, pendingRemovals);
                pendingRemovals.Clear();

                var updated = await UpdateIndexAsync(indexInfosAsArray, change, content, cancellationToken);
                if (updated is false)
                {
                    pendingRemovals.Add(change);
                }
            }
        }

        await RemoveFromIndexAsync(indexInfosAsArray, pendingRemovals);
    }

    private async Task<bool> UpdateIndexAsync(IndexInfo[] indexInfos, ContentChange change, IContentBase content, CancellationToken cancellationToken)
    {
        var applicableIndexInfos = indexInfos.Where(info => info.ContainedObjectTypes.Contains(change.ObjectType)).ToArray();
        if(applicableIndexInfos.Length is 0)
        {
            return true;
        }

        var result = await UpdateIndexAsync(applicableIndexInfos, content, change.ObjectType, cancellationToken);

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
                            await UpdateIndexDescendantsAsync(applicableIndexInfos, descendants, change.ObjectType, cancellationToken)
                    );
                    break;
                case UmbracoObjectTypes.Media:
                    await EnumerateDescendantsByPath<IMedia>(
                        change.ObjectType,
                        content.Key,
                        (id, pageIndex, pageSize, query, ordering) => _mediaService
                            .GetPagedDescendants(id, pageIndex, pageSize, out _, query, ordering)
                            .ToArray(),
                        async descendants =>
                            await UpdateIndexDescendantsAsync(applicableIndexInfos, descendants, change.ObjectType, cancellationToken)
                    );
                    break;
            }
        }

        return result;
    }

    private async Task UpdateIndexDescendantsAsync<T>(IndexInfo[] indexInfos, T[] descendants, UmbracoObjectTypes objectType, CancellationToken cancellationToken)
        where T : IContentBase
    {
        foreach (var descendant in descendants)
        {
            await UpdateIndexAsync(indexInfos, descendant, objectType, cancellationToken);
        }
    }
    
    private async Task<bool> UpdateIndexAsync(IndexInfo[] indexInfos, IContentBase content, UmbracoObjectTypes objectType, CancellationToken cancellationToken)
    {
        var fields = (await _contentIndexingDataCollectionService.CollectAsync(content, false, cancellationToken))?.ToArray();
        if (fields is null)
        {
            return false;
        }

        string?[] cultures = content.AvailableCultures();

        var variations = content.ContentType.VariesBySegment()
            ? cultures
                .SelectMany(culture => content
                    .Properties
                    .SelectMany(property =>
                        property.Values.Where(value => value.Culture.InvariantEquals(culture))
                    )
                    .DistinctBy(value => value.Segment).Select(value => value.Segment)
                    .Select(segment => new Variation(culture, segment))
                ).ToArray()
            : cultures
                .Select(culture => new Variation(culture, null))
                .ToArray();

        foreach (var indexInfo in indexInfos)
        {
            await indexInfo.Indexer.AddOrUpdateAsync(indexInfo.IndexAlias, content.Key, objectType, variations, fields, null);
        }

        return true;
    }

    private async Task RemoveFromIndexAsync(IndexInfo[] indexInfos, IReadOnlyCollection<ContentChange> contentChanges)
    {
        if (contentChanges.Count is 0)
        {
            return;
        }

        foreach (var indexInfo in indexInfos)
        {
            var keys = contentChanges
                .Where(change => indexInfo.ContainedObjectTypes.Contains(change.ObjectType))
                .Select(change => change.Id)
                .ToArray();
            await indexInfo.Indexer.DeleteAsync(indexInfo.IndexAlias, keys);
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
}