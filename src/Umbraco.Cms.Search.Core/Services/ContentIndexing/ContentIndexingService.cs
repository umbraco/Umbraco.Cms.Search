using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.HostedServices;
using Umbraco.Cms.Search.Core.Configuration;
using Umbraco.Cms.Search.Core.Models.Configuration;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Notifications;
using Umbraco.Extensions;

namespace Umbraco.Cms.Search.Core.Services.ContentIndexing;

internal sealed class ContentIndexingService : IContentIndexingService
{
    private readonly IBackgroundTaskQueue _backgroundTaskQueue;
    private readonly IEventAggregator _eventAggregator;
    private readonly ILogger<ContentIndexingService> _logger;
    private readonly IndexOptions _indexOptions;
    private readonly IServiceProvider _serviceProvider;

    public ContentIndexingService(
        IBackgroundTaskQueue backgroundTaskQueue,
        IEventAggregator eventAggregator,
        ILogger<ContentIndexingService> logger,
        IOptions<IndexOptions> indexOptions,
        IServiceProvider serviceProvider)
    {
        _backgroundTaskQueue = backgroundTaskQueue;
        _eventAggregator = eventAggregator;
        _logger = logger;
        _indexOptions = indexOptions.Value;
        _serviceProvider = serviceProvider;
    }

    public void Handle(IEnumerable<ContentChange> changes)
        => _backgroundTaskQueue.QueueBackgroundWorkItem(async cancellationToken => await HandleAsync(changes, cancellationToken));

    public void Rebuild(string indexAlias)
    {
        IndexRegistration? indexRegistration = _indexOptions.GetIndexRegistration(indexAlias);
        if (indexRegistration is null)
        {
            _logger.LogError("Cannot rebuild index - no index registration found for alias: {indexAlias}", indexAlias);
            return;
        }

        _backgroundTaskQueue.QueueBackgroundWorkItem(async cancellationToken => await RebuildAsync(indexRegistration, cancellationToken));
    }

    private async Task HandleAsync(IEnumerable<ContentChange> changes, CancellationToken cancellationToken)
    {
        ContentChange[] changesAsArray = changes as ContentChange[] ?? changes.ToArray();

        IEnumerable<IGrouping<Type, IndexRegistration>> indexRegistrationsByStrategyType = _indexOptions
            .GetIndexRegistrations()
            .GroupBy(r => r.IndexRebuildStrategy);

        foreach (IGrouping<Type, IndexRegistration> group in indexRegistrationsByStrategyType)
        {
            if (TryGetContentChangeStrategy(group.Key, out IContentChangeStrategy? contentChangeStrategy) is false)
            {
                continue;
            }

            IndexInfo[] indexInfos = group
                .Select(g =>
                    TryGetIndexer(g.Indexer, out IIndexer? indexer)
                        ? new IndexInfo(g.IndexAlias, g.ContainedObjectTypes, indexer)
                        : null)
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

    private async Task RebuildAsync(IndexRegistration indexRegistration, CancellationToken cancellationToken)
    {
        if (TryGetIndexRebuildStrategy(indexRegistration.IndexRebuildStrategy, out IIndexRebuildStrategy? rebuildStrategy) is false
            || TryGetIndexer(indexRegistration.Indexer, out IIndexer? indexer) is false)
        {
            return;
        }

        await _eventAggregator.PublishAsync(new IndexRebuildStartingNotification(indexRegistration.IndexAlias), cancellationToken);

        await rebuildStrategy.RebuildAsync(new IndexInfo(indexRegistration.IndexAlias, indexRegistration.ContainedObjectTypes, indexer), cancellationToken);

        await _eventAggregator.PublishAsync(new IndexRebuildCompletedNotification(indexRegistration.IndexAlias), cancellationToken);
    }

    private bool TryGetIndexRebuildStrategy(Type type, [NotNullWhen(true)] out IIndexRebuildStrategy? rebuildStrategy)
    {
        if (_serviceProvider.GetService(type) is IIndexRebuildStrategy resolvedRebuildStrategy)
        {
            rebuildStrategy = resolvedRebuildStrategy;
            return true;
        }

        _logger.LogError($"Could not resolve type {{type}} as {nameof(IIndexRebuildStrategy)}. Make sure the type is registered in the DI.", type.FullName);
        rebuildStrategy = null;
        return false;
    }

    private bool TryGetContentChangeStrategy(Type type, [NotNullWhen(true)] out IContentChangeStrategy? contentChangeStrategy)
    {
        if (_serviceProvider.GetService(type) is IContentChangeStrategy resolvedContentChangeStrategy)
        {
            contentChangeStrategy = resolvedContentChangeStrategy;
            return true;
        }

        contentChangeStrategy = null;
        return false;
    }

    private bool TryGetIndexer(Type type, [NotNullWhen(true)] out IIndexer? indexer)
    {
        if (_serviceProvider.GetService(type) is IIndexer resolvedIndexer)
        {
            indexer = resolvedIndexer;
            return true;
        }

        _logger.LogError($"Could not resolve type {{type}} as {nameof(IIndexer)}. Make sure the type is registered in the DI.", type.FullName);
        indexer = null;
        return false;
    }
}
