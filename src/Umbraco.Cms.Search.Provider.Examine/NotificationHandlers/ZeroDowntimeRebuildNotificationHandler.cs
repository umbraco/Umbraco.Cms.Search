using Examine;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Search.Core.Notifications;
using Umbraco.Cms.Search.Provider.Examine.Services;

namespace Umbraco.Cms.Search.Provider.Examine.NotificationHandlers;

internal sealed class ZeroDowntimeRebuildNotificationHandler :
    INotificationAsyncHandler<IndexRebuildStartingNotification>,
    INotificationAsyncHandler<IndexRebuildCompletedNotification>
{
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

        if (IsShadowIndexHealthy(shadowIndexName))
        {
            _activeIndexManager.CompleteRebuilding(notification.IndexAlias);
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
