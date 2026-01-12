using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Api.Common.ViewModels.Pagination;
using Umbraco.Cms.Search.Core.Configuration;
using Umbraco.Cms.Search.Core.Models.ViewModels;

namespace Umbraco.Cms.Search.Core.Controllers;

[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Search")]
public class SearchApiController : SearchApiControllerBase
{
    private readonly IndexOptions _options;

    public SearchApiController(IOptions<IndexOptions> options) => _options = options.Value;

    [HttpGet("indexes")]
    [ProducesResponseType<IndexViewModel>(StatusCodes.Status200OK)]
    public IActionResult Indexes()
    {
        IndexViewModel[] viewModels = _options.GetIndexRegistrations().Select(x => new IndexViewModel { Name = x.IndexAlias, DocumentCount = 0 }).ToArray();
        return Ok(new PagedViewModel<IndexViewModel> { Items = viewModels, Total = viewModels.Length });
    }
}
