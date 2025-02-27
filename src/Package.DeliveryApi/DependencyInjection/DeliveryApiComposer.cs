using Microsoft.Extensions.DependencyInjection;
using Package.DeliveryApi.Services;
using Package.Services.ContentIndexing;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DeliveryApi;
using Umbraco.Cms.Core.DependencyInjection;

namespace Package.DeliveryApi.DependencyInjection;

public sealed class DeliveryApiComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IApiContentQueryProvider, DeliveryApiContentQueryProvider>();
        builder.Services.AddTransient<IContentIndexer, DeliveryApiContentIndexer>();
    }
}