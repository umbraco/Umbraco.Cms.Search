using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Api.Common.ViewModels.Pagination;
using Umbraco.Cms.Search.Core.Configuration;
using Umbraco.Cms.Search.Core.Models.Configuration;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Models.ViewModels;
using Umbraco.Cms.Search.Core.Services;

namespace Umbraco.Cms.Search.Core.Controllers;

[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Search")]
public class GetAllIndexesApiController : ApiControllerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<GetAllIndexesApiController> _logger;
    private readonly IndexOptions _options;

    public GetAllIndexesApiController(
        IOptions<IndexOptions> options,
        IServiceProvider serviceProvider,
        ILogger<GetAllIndexesApiController> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    [HttpGet("indexes")]
    [ProducesResponseType<PagedViewModel<IndexViewModel>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Indexes()
    {
        List<IndexViewModel> indexes = [];
        foreach (IndexRegistration indexRegistration in _options.GetIndexRegistrations())
        {
            if (TryGetIndexer(_serviceProvider, indexRegistration.Indexer, _logger, out IIndexer? indexer) is false)
            {
                _logger.LogError($"Could not resolve type {{type}} as {nameof(IIndexer)}. Make sure the type is registered in the DI.", indexRegistration.Indexer.FullName);
                continue;
            }

            IndexMetadata indexMetadata = await indexer.GetMetadataAsync(indexRegistration.IndexAlias);

            indexes.Add(
                new IndexViewModel
                {
                    IndexAlias = indexRegistration.IndexAlias,
                    DocumentCount = indexMetadata.DocumentCount,
                    HealthStatus = indexMetadata.HealthStatus,
                });
        }

        return Ok(new PagedViewModel<IndexViewModel> { Items = indexes, Total = indexes.Count });
    }
}
