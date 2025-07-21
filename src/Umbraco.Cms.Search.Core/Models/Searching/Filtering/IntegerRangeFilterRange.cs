namespace Umbraco.Cms.Search.Core.Models.Searching.Filtering;

public record IntegerRangeFilterRange(int? Min, int? Max)
    : RangeFilterRange<int?>(Min, Max)
{
}