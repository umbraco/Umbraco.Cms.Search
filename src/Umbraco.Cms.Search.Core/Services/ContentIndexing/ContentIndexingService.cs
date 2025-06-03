using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.HostedServices;
using Umbraco.Cms.Search.Core.Configuration;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Extensions;

namespace Umbraco.Cms.Search.Core.Services.ContentIndexing;

internal sealed class ContentIndexingService : IContentIndexingService
{
    private readonly IBackgroundTaskQueue _backgroundTaskQueue;
    private readonly ILogger<ContentIndexingService> _logger;
    private readonly IndexOptions _indexOptions;
    private readonly IServiceProvider _serviceProvider;

    public ContentIndexingService(
        IBackgroundTaskQueue backgroundTaskQueue,
        ILogger<ContentIndexingService> logger,
        IOptions<IndexOptions> indexOptions,
        IServiceProvider serviceProvider)
    {
        _backgroundTaskQueue = backgroundTaskQueue;
        _logger = logger;
        _indexOptions = indexOptions.Value;
        _serviceProvider = serviceProvider;
    }

    public void Handle(IEnumerable<ContentChange> changes)
        => _backgroundTaskQueue.QueueBackgroundWorkItem(async cancellationToken => await HandleAsync(changes, cancellationToken));

    private async Task HandleAsync(IEnumerable<ContentChange> changes, CancellationToken cancellationToken)
    {
        var changesAsArray = changes as ContentChange[] ?? changes.ToArray();

        var indexRegistrationsByStrategyType = _indexOptions
            .GetIndexRegistrations()
            .GroupBy(r => r.ContentChangeStrategy);

        foreach (var group in indexRegistrationsByStrategyType)
        {
            if (_serviceProvider.GetService(group.Key) is not IContentChangeStrategy contentChangeStrategy)
            {
                _logger.LogError($"Could not resolve type {{type}} as {nameof(IContentChangeStrategy)}. Make sure the type is registered in the DI.", group.Key.FullName);
                continue;
            }

            var indexInfos = group
                .Select(g =>
                {
                    if (_serviceProvider.GetService(g.Indexer) is not IIndexer indexer)
                    {
                        _logger.LogError($"Could not resolve type {{type}} as {nameof(IIndexer)}. Make sure the type is registered in the DI.", g.Indexer.FullName);
                        return null;
                    }

                    return new IndexInfo(g.IndexAlias, g.ContainedObjectTypes, indexer);
                })
                .WhereNotNull()
                .ToArray();

            if (indexInfos.Length == 0)
            {
                _logger.LogWarning($"Could not resolve any indexes for {nameof(IContentChangeStrategy)} of type {{type}}. Index updates will be skipped.", group.Key.FullName);
                continue;
            }

            await contentChangeStrategy.HandleAsync(indexInfos, changesAsArray, cancellationToken);
        }
    }
}