using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Package;
using Package.Helpers;
using Package.Models.Searching.Faceting;
using Package.Models.Searching.Filtering;
using Package.Models.Searching.Sorting;
using Package.Services;
using Site.ViewModels;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Site.Controllers;

public class SearchController : RenderController
{
    private readonly ISearchService _searchService;
    private readonly IDateTimeOffsetConverter _dateTimeOffsetConverter;

    public SearchController(
        ILogger<RenderController> logger,
        ICompositeViewEngine compositeViewEngine,
        IUmbracoContextAccessor umbracoContextAccessor,
        ISearchService searchService,
        IDateTimeOffsetConverter dateTimeOffsetConverter)
        : base(logger, compositeViewEngine, umbracoContextAccessor)
    {
        _searchService = searchService;
        _dateTimeOffsetConverter = dateTimeOffsetConverter;
    }

    [NonAction]
    public override IActionResult Index()
        => throw new NotImplementedException();

    public async Task<IActionResult> Index(string? query, string[]? filters, string[]? facets, string? culture, string? segment, string? sortBy, string? sortDirection)
    {
        var filterDictionary = SplitParameters(filters);
        var filterValues = filterDictionary?
            .Select(kvp => ParseFilter(kvp.Key, kvp.Value))
            .WhereNotNull()
            .ToArray();

        var facetValues = facets?.Select(f => f.InvariantContains("integer")
            ? new IntegerExactFacet(f)
            : (Facet)new KeywordFacet(f)
        ).ToArray();

        var direction = sortDirection == "asc" ? Direction.Ascending : Direction.Descending;
        Sorter[] sorters = sortBy.IsNullOrWhiteSpace() || sortBy == IndexConstants.FieldNames.Score
            ? [new ScoreSorter(direction)]
            : sortBy.InvariantContains("integer") || sortBy == IndexConstants.FieldNames.Level
                ? [new IntegerSorter(sortBy, direction)]
                : sortBy.InvariantContains("date")
                    ? [new DateTimeOffsetSorter(sortBy, direction)]
                    : sortBy.InvariantContains("dropdown")
                        ? [new KeywordSorter(sortBy, direction)] 
                        : [new StringSorter(sortBy, direction)];

        var result = await _searchService.SearchAsync(query, filterValues, facetValues, sorters, culture, segment, 0, 100);
        
        return CurrentTemplate(
            new SearchViewModel
            {
                Total = result.Total,
                Facets = result.Facets.ToArray(),
                Documents = result.Ids.Select(id => UmbracoContext.Content!.GetById(id)!).ToArray()
            }
        );
    }

    private Dictionary<string, string[]>? SplitParameters(string[]? parameters)
        => parameters?.Any() is true
            ? parameters
                .Select(f => f.Split(':', StringSplitOptions.RemoveEmptyEntries))
                .Where(parts => parts.Length == 2)
                .ToDictionary(part => part[0], part => part[1].Split('|').Except(["*"]).ToArray())
            : null;

    private Filter? ParseFilter(string fieldName, string[] values)
    {
        // range filter?
        if (values.Length == 1)
        {
            var match = Regex.Match(values[0], @"\[(?<lower>\S*),(?<upper>\S*)\]");
            if (match.Success)
            {
                var lower = match.Groups["lower"].Value;
                var upper = match.Groups["upper"].Value;
                if (lower.IsNullOrWhiteSpace() && upper.IsNullOrWhiteSpace())
                {
                    throw new InvalidOperationException("Range filters must supply at least one of the bounds.");
                }

                if (fieldName.InvariantContains("integer"))
                {
                    int? minimum = lower.IsNullOrWhiteSpace() ? null : int.TryParse(lower, out var lowerValue) ? lowerValue : null;
                    int? maximum = upper.IsNullOrWhiteSpace() ? null : int.TryParse(upper, out var upperValue) ? upperValue : null;
                    if (minimum.HasValue is false && maximum.HasValue is false)
                    {
                        throw new InvalidOperationException("Could not parse valid integer range bounds.");
                    }

                    return new IntegerRangeFilter(fieldName, minimum, maximum, false);
                }

                if (fieldName.InvariantContains("decimal"))
                {
                    decimal? minimum = lower.IsNullOrWhiteSpace() ? null : decimal.TryParse(lower, CultureInfo.InvariantCulture, out var lowerValue) ? lowerValue : null;
                    decimal? maximum = upper.IsNullOrWhiteSpace() ? null : decimal.TryParse(upper, CultureInfo.InvariantCulture, out var upperValue) ? upperValue : null;
                    if (minimum.HasValue is false && maximum.HasValue is false)
                    {
                        throw new InvalidOperationException("Could not parse valid decimal range bounds.");
                    }

                    return new DecimalRangeFilter(fieldName, minimum, maximum, false);
                }            

                if (fieldName.InvariantContains("date"))
                {
                    DateTimeOffset? minimum = lower.IsNullOrWhiteSpace() ? null : DateTime.TryParse(lower, CultureInfo.InvariantCulture, out var lowerValue) ? _dateTimeOffsetConverter.ToDateTimeOffset(lowerValue) : null;
                    DateTimeOffset? maximum = upper.IsNullOrWhiteSpace() ? null : DateTime.TryParse(upper, CultureInfo.InvariantCulture, out var upperValue) ? _dateTimeOffsetConverter.ToDateTimeOffset(upperValue) : null;
                    if (minimum.HasValue is false && maximum.HasValue is false)
                    {
                        throw new InvalidOperationException("Could not parse valid date range bounds.");
                    }

                    return new DateTimeOffsetRangeFilter(fieldName, minimum, maximum, false);
                }            

                throw new InvalidOperationException("Unsupported range field type.");
            }
        }

        // exact filter

        if (fieldName.InvariantContains("integer"))
        {
            var fieldValues = values.Select(v => int.TryParse(v, out var i) ? i : int.MinValue).Where(i => i > int.MinValue).ToArray();
            return new IntegerExactFilter(fieldName, fieldValues, false);
        }

        if (fieldName.InvariantContains("decimal"))
        {
            var fieldValues = values.Select(v => decimal.TryParse(v, CultureInfo.InvariantCulture, out var d) ? d : decimal.MinValue).Where(d => d > decimal.MinValue).ToArray();
            return new DecimalExactFilter(fieldName, fieldValues, false);
        }

        if (fieldName.InvariantContains("date"))
        {
            var fieldValues = values.Select(v => DateTime.TryParse(v, CultureInfo.InvariantCulture, out var d) ? _dateTimeOffsetConverter.ToDateTimeOffset(d) : DateTimeOffset.MinValue).Where(d => d > DateTimeOffset.MinValue).ToArray();
            return new DateTimeOffsetExactFilter(fieldName, fieldValues, false);
        }

        return new KeywordFilter(fieldName, values, false);
    }
}
