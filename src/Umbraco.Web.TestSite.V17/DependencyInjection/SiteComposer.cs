using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Search.BackOffice.DependencyInjection;
using Umbraco.Cms.Search.Core.DependencyInjection;
using Umbraco.Cms.Search.DeliveryApi.DependencyInjection;
using Umbraco.Cms.Search.Provider.Examine.DependencyInjection;

namespace Site.DependencyInjection;

public sealed class SiteComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder
            // add core services for search abstractions
            .AddSearchCore()
            // add the Examine search provider
            .AddExamineSearchProvider()
            // add delivery api search implementation
            .AddDeliveryApi()
            .AddDeliveryApiSearch()
            // add the backoffice search implementation
            .AddBackOfficeSearch()
            // force rebuild indexes after startup (awaiting a better solution from Core)
            .RebuildIndexesAfterStartup();

        builder
            // configure the Examine search provider for this site
            .ConfigureExamineSearchProvider()
            // configure System.Text.Json to allow serializing output models
            .ConfigureJsonOptions();
    }
}
