using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Search.Core.Helpers;
using Umbraco.Cms.Search.Core.Models.Searching.Filtering;
using Umbraco.Cms.Search.Core.Models.Searching.Sorting;
using Umbraco.Cms.Search.Core.Services;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DeliveryApi;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.DeliveryApi;
using Umbraco.Cms.Search.Core;
using Umbraco.Extensions;

namespace Umbraco.Cms.Search.DeliveryApi.Services;

internal sealed class DeliveryApiContentQueryProvider : IApiContentQueryProvider
{
    private readonly ISearchService _searchService;
    private readonly IDateTimeOffsetConverter _dateTimeOffsetConverter;
    private readonly ILogger<DeliveryApiContentQueryProvider> _logger;
    private readonly Dictionary<string, FieldType> _fieldTypes;

    public DeliveryApiContentQueryProvider(
        ISearchService searchService,
        ContentIndexHandlerCollection contentIndexHandlerCollection,
        IDateTimeOffsetConverter dateTimeOffsetConverter,
        ILogger<DeliveryApiContentQueryProvider> logger)
    {
        _searchService = searchService;
        _dateTimeOffsetConverter = dateTimeOffsetConverter;
        _logger = logger;

        // build a look-up dictionary of field types by field name
        _fieldTypes = contentIndexHandlerCollection
            .SelectMany(handler => handler.GetFields())
            .DistinctBy(field => field.FieldName)
            .ToDictionary(field => field.FieldName, field => field.FieldType, StringComparer.InvariantCultureIgnoreCase);
    }

    [Obsolete($"Use the {nameof(ExecuteQuery)} method that accepts {nameof(ProtectedAccess)}. Will be removed in V14.")]
    public PagedModel<Guid> ExecuteQuery(
        SelectorOption selectorOption,
        IList<FilterOption> filterOptions,
        IList<SortOption> sortOptions,
        string culture,
        bool preview,
        int skip,
        int take)
        => ExecuteQuery(selectorOption, filterOptions, sortOptions, culture, ProtectedAccess.None, preview, skip, take);
    
    public PagedModel<Guid> ExecuteQuery(
        SelectorOption selectorOption,
        IList<FilterOption> filterOptions,
        IList<SortOption> sortOptions,
        string culture,
        ProtectedAccess protectedAccess,
        bool preview,
        int skip,
        int take)
    {
        if (preview)
        {
            throw new NotImplementedException("TODO: implement draft search");
        }
        
        var filters = new List<Filter>();

        if (selectorOption.Values.Length > 0 && TryCreateFilter(selectorOption.FieldName, FilterOperation.Is, selectorOption.Values, out var selectorOptionFilter))
        {
            filters.Add(selectorOptionFilter);
        }

        foreach (var filterOption in filterOptions)
        {
            if (TryCreateFilter(filterOption.FieldName, filterOption.Operator, filterOption.Values, out var filterOptionFilter))
            {
                filters.Add(filterOptionFilter);
            }
        }

        var sorters = sortOptions
            .Select(sortOption => TryCreateSorter(sortOption.FieldName, sortOption.Direction, out var sorter)
                ? sorter
                : null
            )
            .WhereNotNull()
            .ToArray();
        
        var result = _searchService
            .SearchAsync(null, filters, null, sorters, culture, null, skip, take)
            .GetAwaiter()
            .GetResult();

        return new PagedModel<Guid>(result.Total, result.Ids);
    }

    public SelectorOption AllContentSelectorOption()
        => new() { FieldName = string.Empty, Values = [] };

    private bool TryCreateFilter(string fieldName, FilterOperation filterOperation, string[] values, [NotNullWhen(true)] out Filter? filter)
    {
        if (values.Length is 0)
        {
            filter = null;
            return false;
        }

        if (_fieldTypes.TryGetValue(fieldName, out var fieldType) is false)
        {
            _logger.LogWarning(
                "Filter implementation for field name {FieldName} does not match an index handler implementation, cannot resolve field type.",
                fieldName);
            filter = null;
            return false;
        }

        fieldName = MapSystemFieldName(fieldName);
        
        switch (fieldType)
        {
            case FieldType.StringRaw:
                filter = filterOperation switch
                {
                    FilterOperation.Is or FilterOperation.IsNot => new KeywordFilter(fieldName, values, filterOperation is FilterOperation.IsNot),
                    _ => null
                };
                break;
            case FieldType.StringAnalyzed:
            case FieldType.StringSortable:
                filter = filterOperation switch
                {
                    FilterOperation.Is or FilterOperation.IsNot => new KeywordFilter(fieldName, values, filterOperation is FilterOperation.IsNot),
                    FilterOperation.Contains or FilterOperation.DoesNotContain => new TextFilter(fieldName, values, filterOperation is FilterOperation.DoesNotContain),
                    _ => null
                };
                break;
            case FieldType.Number:
                var decimalValues = values
                    .Select(v => decimal.TryParse(v, CultureInfo.InvariantCulture, out var d) ? d : decimal.MinValue)
                    .Where(d => d > decimal.MinValue)
                    .ToArray();
                if (decimalValues.Length is 0)
                {
                    _logger.LogWarning("Numeric filter for field name {FieldName} did not yield any numeric values.", fieldName);
                    filter = null;
                    return false;
                }
                filter = filterOperation switch
                {
                    FilterOperation.Is or FilterOperation.IsNot => new DecimalExactFilter(fieldName, decimalValues, filterOperation is FilterOperation.IsNot),
                    FilterOperation.LessThan => new DecimalRangeFilter(fieldName, null, decimalValues[0] - 0.001m, false),
                    FilterOperation.LessThanOrEqual => new DecimalRangeFilter(fieldName, null, decimalValues[0], false),
                    FilterOperation.GreaterThan => new DecimalRangeFilter(fieldName, decimalValues[0] + 0.001m, null, false),
                    FilterOperation.GreaterThanOrEqual => new DecimalRangeFilter(fieldName, decimalValues[0], null, false),
                    _ => null
                };
                break;
            case FieldType.Date:
                var dateTimeOffsetValues = values
                    .Select(v => DateTime.TryParse(v, CultureInfo.InvariantCulture, out var d) ? d : DateTime.MinValue)
                    .Where(d => d > DateTime.MinValue)
                    .Select(_dateTimeOffsetConverter.ToDateTimeOffset)
                    .ToArray();
                if (dateTimeOffsetValues.Length is 0)
                {
                    _logger.LogWarning("Date filter for field name {FieldName} did not yield any DateTimeOffset values.", fieldName);
                    filter = null;
                    return false;
                }
                filter = filterOperation switch
                {
                    FilterOperation.Is or FilterOperation.IsNot => new DateTimeOffsetExactFilter(fieldName, dateTimeOffsetValues, filterOperation is FilterOperation.IsNot),
                    FilterOperation.LessThan => new DateTimeOffsetRangeFilter(fieldName, null, dateTimeOffsetValues[0].AddMilliseconds(-1), false),
                    FilterOperation.LessThanOrEqual => new DateTimeOffsetRangeFilter(fieldName, null, dateTimeOffsetValues[0], false),
                    FilterOperation.GreaterThan => new DateTimeOffsetRangeFilter(fieldName, dateTimeOffsetValues[0].AddMilliseconds(1), null, false),
                    FilterOperation.GreaterThanOrEqual => new DateTimeOffsetRangeFilter(fieldName, dateTimeOffsetValues[0], null, false),
                    _ => null
                };
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(fieldType), fieldType, null);
        }

        return filter != null;
    }

    private bool TryCreateSorter(string fieldName, Direction direction, [NotNullWhen(true)] out Sorter? sorter)
    {
        if (_fieldTypes.TryGetValue(fieldName, out var fieldType) is false)
        {
            _logger.LogWarning(
                "Sorter implementation for field name {FieldName} does not match an index handler implementation, cannot resolve field type.",
                fieldName);
            sorter = null;
            return false;
        }

        fieldName = MapSystemFieldName(fieldName);

        if (fieldName is IndexConstants.FieldNames.Level or IndexConstants.FieldNames.SortOrder)
        {
            sorter = new IntegerSorter(fieldName, direction);
            return true;
        }

        sorter = fieldType switch
        {
            FieldType.StringRaw => new KeywordSorter(fieldName, direction),
            FieldType.StringAnalyzed or FieldType.StringSortable => new StringSorter(fieldName, direction),
            FieldType.Number => new DecimalSorter(fieldName, direction),
            FieldType.Date => new DateTimeOffsetSorter(fieldName, direction),
            _ => throw new ArgumentOutOfRangeException(nameof(fieldType), fieldType, null)
        };

        return true;
    }

    // hardcoded mapping from the old Delivery API fields to the search abstraction ones
    private static string MapSystemFieldName(string fieldName)
        => fieldName switch
        {
            // AncestorsSelectorIndexer:
            "itemId" => IndexConstants.FieldNames.Id,
            // ChildrenSelectorIndexer:
            "parentId" => IndexConstants.FieldNames.ParentId,
            // DescendantsSelectorIndexer:
            // TODO: this is somewhat wrong... PathIds equals ancestors-or-self, but the Delivery API queries for ancestors only
            "ancestorIds" => IndexConstants.FieldNames.PathIds,
            // ContentTypeFilterIndexer:
            "contentType" => IndexConstants.FieldNames.ContentType,
            // NameFilterIndexer or NameSortIndexer:
            "name" or "sortName" => IndexConstants.FieldNames.Name,
            // CreateDateSortIndexer
            "createDate" => IndexConstants.FieldNames.CreateDate,
            // UpdateDateSortIndexer
            "updateDate" => IndexConstants.FieldNames.UpdateDate,
            // LevelSortIndexer
            "level" => IndexConstants.FieldNames.Level,
            // SortOrderSortIndexer
            "sortOrder" => IndexConstants.FieldNames.SortOrder,
            _ => fieldName
        };
}