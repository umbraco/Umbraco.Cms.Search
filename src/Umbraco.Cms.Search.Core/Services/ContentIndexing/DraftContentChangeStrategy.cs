using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Extensions;

namespace Umbraco.Cms.Search.Core.Services.ContentIndexing;

public class DraftContentChangeStrategy : IDraftContentChangeStrategy
{
    private readonly IContentIndexingDataCollectionService _contentIndexingDataCollectionService;
    private readonly IContentService _contentService;

    public DraftContentChangeStrategy(IContentIndexingDataCollectionService contentIndexingDataCollectionService, IContentService contentService)
    {
        _contentIndexingDataCollectionService = contentIndexingDataCollectionService;
        _contentService = contentService;
    }

    public async Task HandleAsync(IEnumerable<IndexInfo> indexInfos, IEnumerable<ContentChange> changes, CancellationToken cancellationToken)
    {
        var indexInfosAsArray = indexInfos as IndexInfo[] ?? indexInfos.ToArray();

        var pendingRemovals = new List<Guid>();
        foreach (var change in changes.Where(change => change.PublishStateAffected is false))
        {
            if (change.ChangeType is ContentChangeType.Remove)
            {
                pendingRemovals.Add(change.Key);
            }
            else
            {
                var content = _contentService.GetById(change.Key);
                if (content is null)
                {
                    pendingRemovals.Add(change.Key);
                    continue;
                }

                await RemoveFromIndexAsync(indexInfosAsArray, pendingRemovals);
                pendingRemovals.Clear();

                var updated = await UpdateIndexAsync(indexInfosAsArray, content, cancellationToken);
                if (updated is false)
                {
                    pendingRemovals.Add(content.Key);
                }
            }
        }

        await RemoveFromIndexAsync(indexInfosAsArray, pendingRemovals);
    }

    private async Task<bool> UpdateIndexAsync(IndexInfo[] indexInfos, IContent content, CancellationToken cancellationToken)
    {
        var fields = (await _contentIndexingDataCollectionService.CollectAsync(content, false, cancellationToken))?.ToArray();
        if (fields is null)
        {
            return false;
        }

        string?[] cultures = content.ContentType.VariesByCulture()
            ? content.AvailableCultures.ToArray()
            : [null];

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
            await indexInfo.IndexService.AddOrUpdateAsync(indexInfo.IndexAlias, content.Key, variations, fields, null);
        }

        return true;
    }

    private async Task RemoveFromIndexAsync(IndexInfo[] indexInfos, IReadOnlyCollection<Guid> keys)
    {
        foreach (var indexInfo in indexInfos)
        {
            await indexInfo.IndexService.DeleteAsync(indexInfo.IndexAlias, keys);
        }
    }
}