using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Search.Core.Services;
using Umbraco.Cms.Search.Provider.InMemory.Services;

namespace Umbraco.Cms.Search.Provider.InMemory.DependencyInjection;

public class Composer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services
            .AddSingleton<DataStore>()
            .AddTransient<IIndexService, IndexService>()
            .AddTransient<ISearchService, SearchService>();
}