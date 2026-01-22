using System.Diagnostics.CodeAnalysis;
using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Api.Common.ViewModels.Pagination;
using Umbraco.Cms.Search.Core.Configuration;
using Umbraco.Cms.Search.Core.Models.Configuration;
using Umbraco.Cms.Search.Core.Models.ViewModels;
using Umbraco.Cms.Search.Core.Services;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;

namespace Umbraco.Cms.Search.Core.Controllers;

[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Search")]
public class SearchApiController : SearchApiControllerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SearchApiController> _logger;
    private readonly IContentIndexingService _contentIndexingService;
    private readonly IndexOptions _options;

    public SearchApiController(IOptions<IndexOptions> options, IServiceProvider serviceProvider, ILogger<SearchApiController> logger, IContentIndexingService contentIndexingService)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _contentIndexingService = contentIndexingService;
        _options = options.Value;
    }

    [HttpGet("indexes")]
    [ProducesResponseType<PagedViewModel<IndexViewModel>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Indexes()
    {
        List<IndexViewModel> indexes = [];
        foreach (IndexRegistration indexRegistration in _options.GetIndexRegistrations())
        {
            if (TryGetIndexer(indexRegistration.Indexer, out IIndexer? indexer) is false)
            {
                _logger.LogError($"Could not resolve type {{type}} as {nameof(IIndexer)}. Make sure the type is registered in the DI.", indexRegistration.Indexer.FullName);
                continue;
            }

            indexes.Add(
                new IndexViewModel
                {
                    IndexAlias = indexRegistration.IndexAlias,
                    DocumentCount = await indexer.GetDocumentCountAsync(indexRegistration.IndexAlias),
                    HealthStatus = HealthStatus.Healthy,
                });
        }

        return Ok(new PagedViewModel<IndexViewModel> { Items = indexes, Total = indexes.Count });
    }

    [HttpPut("rebuild")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Rebuild(string indexAlias)
    {
        if (string.IsNullOrWhiteSpace(indexAlias))
        {
            return BadRequest("The indexAlias parameter must be provided and cannot be empty.");
        }

        // Check if the index exists before calling the service
        IndexRegistration? indexRegistration = _options.GetIndexRegistration(indexAlias);
        if (indexRegistration is null)
        {
            return NotFound("The specified index alias was not found.");
        }

        _contentIndexingService.Rebuild(indexAlias);
        return Ok();
    }

    private bool TryGetIndexer(Type type, [NotNullWhen(true)] out IIndexer? indexer)
    {
        if (_serviceProvider.GetService(type) is IIndexer resolvedIndexer)
        {
            indexer = resolvedIndexer;
            return true;
        }

        _logger.LogError($"Could not resolve type {{type}} as {nameof(IIndexer)}. Make sure the type is registered in the DI.", type.FullName);
        indexer = null;
        return false;
    }
}
