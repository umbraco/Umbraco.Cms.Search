using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.HostedServices;
using Umbraco.Cms.Search.Core.Configuration;
using Umbraco.Cms.Search.Core.Models.Configuration;
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

    public void Rebuild(string indexAlias)
    {
        var indexRegistration = _indexOptions.GetIndexRegistration(indexAlias);
        if (indexRegistration is null)
        {
            _logger.LogError("Cannot rebuild index - no index registration found for alias: {indexAlias}", indexAlias);
            return;
        }
 
        _backgroundTaskQueue.QueueBackgroundWorkItem(async cancellationToken => await RebuildAsync(indexRegistration, cancellationToken));
    }
    
    private async Task HandleAsync(IEnumerable<ContentChange> changes, CancellationToken cancellationToken)
    {
        var changesAsArray = changes as ContentChange[] ?? changes.ToArray();

        var indexRegistrationsByStrategyType = _indexOptions
            .GetIndexRegistrations()
            .GroupBy(r => r.ContentChangeStrategy);

        foreach (var group in indexRegistrationsByStrategyType)
        {
            if (TryGetContentChangeStrategy(group.Key, out var contentChangeStrategy) is false)
            {
                continue;
            }

            var indexInfos = group
                .Select(g =>
                    TryGetIndexer(g.Indexer, out var indexer)
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
        if (TryGetContentChangeStrategy(indexRegistration.ContentChangeStrategy, out var contentChangeStrategy) is false
            || TryGetIndexer(indexRegistration.Indexer, out var indexer) is false)
        {
            return;
        }

        await contentChangeStrategy.RebuildAsync(new IndexInfo(indexRegistration.IndexAlias, indexRegistration.ContainedObjectTypes, indexer), cancellationToken);
    }

    private bool TryGetContentChangeStrategy(Type type, [NotNullWhen(true)] out IContentChangeStrategy? contentChangeStrategy)
    {
        if (_serviceProvider.GetService(type) is IContentChangeStrategy resolvedContentChangeStrategy)
        {
            contentChangeStrategy = resolvedContentChangeStrategy;
            return true;
        }

        _logger.LogError($"Could not resolve type {{type}} as {nameof(IContentChangeStrategy)}. Make sure the type is registered in the DI.", type.FullName);
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