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

        ISearchResults results = index.Searcher
            .CreateQuery()
            .Field(
                FieldNameHelper.FieldName(Core.Constants.FieldNames.Id, Constants.FieldValues.Keywords),
                documentId)
            .Execute();

        ISearchResult? result = results.FirstOrDefault();
        if (result is null)
        {
            return NotFound(
                $"Could not find document with key '{documentKey}'{(culture is not null ? $" and culture '{culture}'" : string.Empty)} in index '{indexAlias}'");
        }

        var viewModel = new DocumentViewModel
        {
            Key = documentKey,
            Culture = culture,
            Fields = result.AllValues
                .Select(kvp => ParseField(kvp.Key, kvp.Value))
                .ToList()
                .AsReadOnly(),
        };

        return Ok(viewModel);
    }

    private static FieldViewModel ParseField(string fieldName, IEnumerable<string> values)
    {
        // Strip "Field_" prefix if present
        var cleanName = fieldName.StartsWith("Field_")
            ? fieldName[6..]
            : fieldName;

        // Extract field type suffix (e.g., "_keywords", "_texts")
        string? fieldType = null;
        var parts = cleanName.Split('_');
        if (parts.Length > 1)
        {
            var lastPart = parts[^1];
            if (IsFieldTypeSuffix(lastPart))
            {
                fieldType = lastPart;
                cleanName = string.Join("_", parts[..^1]);
            }
        }

        return new FieldViewModel { Name = cleanName, Type = fieldType, Values = values.ToList().AsReadOnly(), };
    }

    private static bool IsFieldTypeSuffix(string suffix) =>
        suffix is Constants.FieldValues.Keywords or Constants.FieldValues.Texts or Constants.FieldValues.TextsR1
            or Constants.FieldValues.TextsR2 or Constants.FieldValues.TextsR3 or Constants.FieldValues.Integers
            or Constants.FieldValues.Decimals or Constants.FieldValues.DateTimeOffsets;
}
