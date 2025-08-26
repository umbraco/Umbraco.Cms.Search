using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Search.Core.Configuration;
using Umbraco.Cms.Search.Core.Models.Configuration;

namespace Umbraco.Cms.Search.Core.Services;

internal sealed class SearcherResolver : ISearcherResolver
{
    private readonly IndexOptions _indexOptions;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SearcherResolver> _logger;

    public SearcherResolver(IOptions<IndexOptions> indexOptions, IServiceProvider serviceProvider, ILogger<SearcherResolver> logger)
    {
        _indexOptions = indexOptions.Value;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public ISearcher? GetSearcher(string indexAlias)
    {
        IndexRegistration? indexRegistration = _indexOptions.GetIndexRegistration(indexAlias);
        if (indexRegistration is null)
        {
            _logger.LogWarning("No index registration was found for index alias: {indexAlias}", indexAlias);
            return null;
        }

        if (_serviceProvider.GetService(indexRegistration.Searcher) is not ISearcher searcher)
        {
            _logger.LogError($"Could not resolve type {{type}} as {nameof(ISearcher)}. Make sure the type is registered in the DI.", indexRegistration.Searcher.FullName);
            return null;
        }

        return searcher;
    }
}
