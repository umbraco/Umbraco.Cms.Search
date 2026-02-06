using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Search.Core.Configuration;
using Umbraco.Cms.Search.Core.Models.Configuration;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;

namespace Umbraco.Cms.Search.Core.Controllers;

[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Search")]
public class RebuildIndexApiController : ApiControllerBase
{
    private readonly IContentIndexingService _contentIndexingService;
    private readonly IndexOptions _options;

    public RebuildIndexApiController(IContentIndexingService contentIndexingService, IOptions<IndexOptions> options)
    {
        _contentIndexingService = contentIndexingService;
        _options = options.Value;
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
}
