using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Search.Core.Cache.PublicAccess;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;

namespace Umbraco.Cms.Search.Core.NotificationHandlers;

internal sealed class PublicAccessIndexingNotificationHandler : IndexingNotificationHandlerBase, INotificationAsyncHandler<PublicAccessCacheRefresherNotification>
{
    private readonly IContentIndexingService _contentIndexingService;

    public PublicAccessIndexingNotificationHandler(ICoreScopeProvider coreScopeProvider, IContentIndexingService contentIndexingService)
        : base(coreScopeProvider)
        => _contentIndexingService = contentIndexingService;

    public Task HandleAsync(PublicAccessCacheRefresherNotification notification, CancellationToken cancellationToken)
    {
        var payloads = GetNotificationPayloads<PublicAccessCacheRefresher.JsonPayload>(notification);
        var changes = payloads
            .Select(payload => new ContentChange(payload.ProtectedContentKey, ContentChangeType.RefreshWithDescendants, true))
            .ToArray();

        ExecuteDeferred(() => _contentIndexingService.Handle(changes));

        return Task.CompletedTask;
    }
}