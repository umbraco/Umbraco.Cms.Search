using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Site.NotificationHandlers;

internal sealed class EnableSegmentsNotificationHandler : INotificationHandler<ServerVariablesParsingNotification>
{
    public void Handle(ServerVariablesParsingNotification notification)
    {
        if (!notification.ServerVariables.TryGetValue("umbracoSettings", out var umbracoSettingsObject)
            || umbracoSettingsObject is not IDictionary<string, object> umbracoSettings)
        {
            umbracoSettings = new Dictionary<string, object>();
            notification.ServerVariables.Add("umbracoSettings", umbracoSettings);
        }

        umbracoSettings["showAllowSegmentationForDocumentTypes"] = true;
    }
}
