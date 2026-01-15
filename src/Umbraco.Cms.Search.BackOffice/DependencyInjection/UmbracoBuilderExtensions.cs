using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Search.BackOffice.Services;

namespace Umbraco.Cms.Search.BackOffice.DependencyInjection;

public static class UmbracoBuilderExtensions
{
    public static IUmbracoBuilder AddBackOfficeSearch(this IUmbracoBuilder builder)
    {
        builder.Services.Replace(ServiceDescriptor.Singleton<IIndexedEntitySearchService, IndexedEntitySearchService>());
        builder.Services.Replace(ServiceDescriptor.Singleton<IContentSearchService, ContentSearchService>());
        builder.Services.Replace(ServiceDescriptor.Singleton<IMediaSearchService, MediaSearchService>());

        return builder;
    }
}
