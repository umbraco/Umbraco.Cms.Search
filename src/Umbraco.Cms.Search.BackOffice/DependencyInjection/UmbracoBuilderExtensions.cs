using Microsoft.Extensions.DependencyInjection.Extensions;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Search.BackOffice.Services;

namespace Umbraco.Cms.Search.BackOffice.DependencyInjection;

public static class UmbracoBuilderExtensions
{
    public static IUmbracoBuilder AddBackOfficeSearch(this IUmbracoBuilder builder)
    {
        builder.Services.TryAddSingleton<IIndexedEntitySearchService, IndexedEntitySearchService>();
        builder.Services.TryAddSingleton<IContentSearchService, ContentSearchService>();
        builder.Services.TryAddSingleton<IMediaSearchService, MediaSearchService>();

        return builder;
    }
}
