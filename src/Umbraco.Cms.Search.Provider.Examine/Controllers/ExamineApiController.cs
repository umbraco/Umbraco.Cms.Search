using Asp.Versioning;
using Examine;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Search.Provider.Examine.Helpers;
using Umbraco.Cms.Search.Provider.Examine.Models.ViewModels;

namespace Umbraco.Cms.Search.Provider.Examine.Controllers;

[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Examine")]
public class ExamineApiController : ExamineApiControllerBase
{
    private readonly IExamineManager _examineManager;

    public ExamineApiController(IExamineManager examineManager) => _examineManager = examineManager;

    [HttpGet("{indexAlias}/document/{documentKey:guid}")]
    [ProducesResponseType<DocumentViewModel>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetDocument(string indexAlias, Guid documentKey, [FromQuery] string? culture = null)
    {
        if (_examineManager.TryGetIndex(indexAlias, out IIndex? index) is false)
        {
            return NotFound($"Could not find index with alias '{indexAlias}'");
        }

        var documentId = DocumentIdHelper.CalculateDocumentId(documentKey, culture);

        // Search for the specific document by its ID
        ISearchResults results = index.Searcher
            .CreateQuery()
            .Id(documentId)
            .Execute();

        ISearchResult? result = results.FirstOrDefault();
        if (result is null)
        {
            return NotFound($"Could not find document with key '{documentKey}'{(culture is not null ? $" and culture '{culture}'" : string.Empty)} in index '{indexAlias}'");
        }

        var viewModel = new DocumentViewModel
        {
            Key = documentKey,
            Culture = culture,
            Fields = result.AllValues.ToDictionary(
                kvp => kvp.Key,
                kvp => (IReadOnlyCollection<string>)kvp.Value.ToList().AsReadOnly()),
        };

        return Ok(viewModel);
    }
}
