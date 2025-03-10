using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Site.NotificationHandlers;
using Umbraco.Cms.Api.Delivery.Configuration;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Search.BackOffice.DependencyInjection;
using Umbraco.Cms.Search.Core.DependencyInjection;
using Umbraco.Cms.Search.DeliveryApi.DependencyInjection;
using Umbraco.Cms.Search.Provider.InMemory.DependencyInjection;

namespace Site.DependencyInjection;

public sealed class SiteComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder
            .AddNotificationHandler<ServerVariablesParsingNotification, EnableSegmentsNotificationHandler>()
            .AddNotificationHandler<SendingContentNotification, CreateSegmentsNotificationHandler>()
            .AddNotificationHandler<UmbracoApplicationStartedNotification, IndexBuildingNotificationHandler>();

        builder.Services.ConfigureOptions<ConfigureCustomMemberLoginPath>();
        builder.Services.ConfigureOptions<ConfigureUmbracoMemberAuthenticationDeliveryApiSwaggerGenOptions>();

        builder
            // add core services for search abstractions
            .AddSearchCore()
            // add the in-memory search provider
            .AddInMemorySearchProvider()
            // use the search abstractions to perform backoffice search
            .AddBackOfficeSearch()
            // use the search abstractions to perform Delivery API queries
            .AddDeliveryApiSearch();
    }

    private class ConfigureCustomMemberLoginPath : IConfigureNamedOptions<CookieAuthenticationOptions>
    {
        public void Configure(string? name, CookieAuthenticationOptions options)
        {
            if (name != IdentityConstants.ApplicationScheme)
            {
                return;
            }

            Configure(options);
        }

        // replace options.LoginPath with the path you want to use for default member logins
        public void Configure(CookieAuthenticationOptions options)
            => options.LoginPath = "/login";
    }
}

