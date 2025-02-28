using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DeliveryApi;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Search.DeliveryApi.Services;

namespace Umbraco.Cms.Search.DeliveryApi.DependencyInjection;

public sealed class DeliveryApiComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IApiContentQueryProvider, DeliveryApiContentQueryProvider>();
        builder.Services.AddTransient<IContentIndexer, DeliveryApiContentIndexer>();
    }
}