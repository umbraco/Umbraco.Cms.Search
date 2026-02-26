using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Search.Core.Configuration;
using Umbraco.Cms.Search.Core.Models.Configuration;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Models.ViewModels;
using Umbraco.Cms.Search.Core.Services;

namespace Umbraco.Cms.Search.Core.Controllers;

[ApiVersion("1.0")]
public class GetIndexApiController : ApiControllerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<GetAllIndexesApiController> _logger;
    private readonly IndexOptions _options;

    public GetIndexApiController(
        IOptions<IndexOptions> options,
        IServiceProvider serviceProvider,
        ILogger<GetAllIndexesApiController> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    [HttpGet("indexes/{indexAlias}")]
    [ProducesResponseType<IndexViewModel>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Index(string indexAlias)
    {
        if (string.IsNullOrWhiteSpace(indexAlias))
        {
            return BadRequest("The indexAlias parameter must be provided and cannot be empty.");
        }

        ContentIndexRegistration? indexRegistration = _options.GetContentIndexRegistration(indexAlias);
        if (indexRegistration is null)
        {
            return NotFound("The specified index alias was not found.");
        }

        if (TryGetIndexer(_serviceProvider, indexRegistration.Indexer, _logger, out IIndexer? indexer) is false)
        {
            return NotFound("Could not resolve the indexer for the specified index.");
        }

        IndexMetadata indexMetadata = await indexer.GetMetadataAsync(indexRegistration.IndexAlias);

        return Ok(new IndexViewModel
        {
            IndexAlias = indexRegistration.IndexAlias,
            DocumentCount = indexMetadata.DocumentCount,
            HealthStatus = indexMetadata.HealthStatus,
        });
    }
}
