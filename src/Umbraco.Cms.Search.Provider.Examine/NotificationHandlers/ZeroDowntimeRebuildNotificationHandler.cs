using Examine;
using Examine.Lucene.Providers;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Search.Core.Notifications;
using Umbraco.Cms.Search.Provider.Examine.Services;

namespace Umbraco.Cms.Search.Provider.Examine.NotificationHandlers;

internal sealed class ZeroDowntimeRebuildNotificationHandler :
    INotificationAsyncHandler<IndexRebuildStartingNotification>,
    INotificationAsyncHandler<IndexRebuildCompletedNotification>
{
    private static readonly TimeSpan CommitTimeout = TimeSpan.FromSeconds(30);

    private readonly IActiveIndexManager _activeIndexManager;
    private readonly IExamineManager _examineManager;
    private readonly ILogger<ZeroDowntimeRebuildNotificationHandler> _logger;

    public ZeroDowntimeRebuildNotificationHandler(
        IActiveIndexManager activeIndexManager,
        IExamineManager examineManager,
        ILogger<ZeroDowntimeRebuildNotificationHandler> logger)
    {
        _activeIndexManager = activeIndexManager;
        _examineManager = examineManager;
        _logger = logger;
    }

    public Task HandleAsync(IndexRebuildStartingNotification notification, CancellationToken cancellationToken)
    {
        _activeIndexManager.StartRebuilding(notification.IndexAlias);
        return Task.CompletedTask;
    }

    public Task HandleAsync(IndexRebuildCompletedNotification notification, CancellationToken cancellationToken)
    {
        var shadowIndexName = _activeIndexManager.ResolveShadowIndexName(notification.IndexAlias);

        // Examine's LuceneIndex.IndexItems() commits asynchronously. We must wait for the
        // commit to complete before checking document count, otherwise we'll see 0 documents
        // and incorrectly cancel the swap.
        WaitForShadowCommit(shadowIndexName);

        if (IsShadowIndexHealthy(shadowIndexName))
        {
            _activeIndexManager.CompleteRebuilding(notification.IndexAlias);
            ClearShadowIndex(notification.IndexAlias);
        }
        else
        {
            _logger.LogWarning(
                "Shadow index {ShadowIndex} is empty or unhealthy after rebuild of {IndexAlias}. Cancelling swap.",
                shadowIndexName,
                notification.IndexAlias);
            _activeIndexManager.CancelRebuilding(notification.IndexAlias);
        }

        return Task.CompletedTask;
    }

    private void WaitForShadowCommit(string physicalIndexName)
    {
        if (_examineManager.TryGetIndex(physicalIndexName, out IIndex? index) is false
            || index is not LuceneIndex luceneIndex)
        {
            return;
        }

        // If documents are already visible, the commit has already happened.
        if (index is IIndexStats stats && stats.GetDocumentCount() > 0)
        {
            return;
        }

        var committed = false;
        void OnCommitted(object? sender, EventArgs e) => committed = true;
        luceneIndex.IndexCommitted += OnCommitted;

        try
        {
            // Re-check after subscribing to avoid a race where the commit happened
            // between the initial check and subscribing to the event.
            if (index is IIndexStats statsAfterSubscribe && statsAfterSubscribe.GetDocumentCount() > 0)
            {
                return;
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            while (!committed && stopwatch.Elapsed < CommitTimeout)
            {
                Thread.Sleep(100);
            }

            if (!committed)
            {
                _logger.LogWarning(
                    "Timed out waiting for shadow index {ShadowIndex} to commit after rebuild",
                    physicalIndexName);
            }
        }
        finally
        {
            luceneIndex.IndexCommitted -= OnCommitted;
        }
    }

    private void ClearShadowIndex(string indexAlias)
    {
        var shadowIndexName = _activeIndexManager.ResolveShadowIndexName(indexAlias);

        if (_examineManager.TryGetIndex(shadowIndexName, out IIndex? index) is false)
        {
            return;
        }

        _logger.LogInformation("Clearing shadow index {ShadowIndex} after successful swap for {IndexAlias}.", shadowIndexName, indexAlias);
        index.CreateIndex();
    }

    private bool IsShadowIndexHealthy(string physicalIndexName)
    {
        if (_examineManager.TryGetIndex(physicalIndexName, out IIndex? index) is false)
        {
            return false;
        }

        if (index.IndexExists() is false)
        {
            return false;
        }

        if (index is IIndexStats stats && stats.GetDocumentCount() > 0)
        {
            return true;
        }

        return false;
    }
}
