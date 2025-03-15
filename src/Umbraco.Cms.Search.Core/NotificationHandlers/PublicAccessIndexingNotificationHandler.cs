using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;

namespace Umbraco.Cms.Search.Core.NotificationHandlers;

internal sealed class PublicAccessIndexingNotificationHandler : IndexingNotificationHandlerBase, INotificationAsyncHandler<PublicAccessDetailedCacheRefresherNotification>
{
    private readonly IContentIndexingService _contentIndexingService;

    public PublicAccessIndexingNotificationHandler(ICoreScopeProvider coreScopeProvider, IContentIndexingService contentIndexingService)
        : base(coreScopeProvider)
        => _contentIndexingService = contentIndexingService;

    public Task HandleAsync(PublicAccessDetailedCacheRefresherNotification notification, CancellationToken cancellationToken)
    {
        var payloads = GetNotificationPayloads<PublicAccessDetailedCacheRefresher.JsonPayload>(notification);
        var changes = payloads
            .Select(payload => ContentChange.Document(payload.ProtectedContentKey, ChangeImpact.RefreshWithDescendants, ContentState.Published))
            .ToArray();

        ExecuteDeferred(() => _contentIndexingService.Handle(changes));

        return Task.CompletedTask;
    }
}