using Umbraco.Cms.Search.Core.Models.Searching.Filtering;

namespace Umbraco.Cms.Search.Provider.Examine.Extensions;

// TODO KJA: CLEAN UP
internal static class FilterExtensions
{
    // internal static IntegerNonNullableRangeFilter ToNonNullableRangeFilter(this IntegerRangeFilter filter)
    // {
    //     return new IntegerNonNullableRangeFilter(filter.FieldName, filter.Ranges.Select(x => new IntegerRangeFilterRange(x.MinValue ?? int.MinValue, x.MaxValue ?? int.MaxValue)).ToArray(), filter.Negate);
    // }
    //
    // internal static DecimalNonNullableRangeFilter ToNonNullableRangeFilter(this DecimalRangeFilter filter)
    // {
    //     return new DecimalNonNullableRangeFilter(filter.FieldName, filter.Ranges.Select(x => new FilterRange<double>((double?)x.MinimumValue ?? double.MinValue, (double?)x.MaximumValue ?? double.MaxValue)).ToArray(), filter.Negate);
    // }
    //
    // internal static DateTimeNonNullableRangeFilter ToNonNullableRangeFilter(this DateTimeOffsetRangeFilter filter)
    // {
    //     return new DateTimeNonNullableRangeFilter(filter.FieldName, filter.Ranges.Select(x => new FilterRange<DateTime>(x.MinimumValue?.DateTime ?? DateTime.MinValue, x.MaximumValue?.DateTime ?? DateTime.MaxValue)).ToArray(), filter.Negate);
    // }
    //
    // internal static DoubleExactFilter ToDoubleExactFilter(this DecimalExactFilter filter)
    // {
    //     return new DoubleExactFilter(filter.FieldName, filter.Values.Select(x => (double) x).ToArray(), filter.Negate);
    // }
    //
    // internal static DateTimeExactFilter ToDateTimeExactFilter(this DateTimeOffsetExactFilter filter)
    // {
    //     return new DateTimeExactFilter(filter.FieldName, filter.Values.Select(x =>  x.DateTime).ToArray(), filter.Negate);
    // }
}

internal record DoubleExactFilter(string FieldName, double[] Values, bool Negate)
    : ExactFilter<double>(FieldName, Values, Negate)
{
}

internal record DateTimeExactFilter(string FieldName, DateTime[] Values, bool Negate)
    : ExactFilter<DateTime>(FieldName, Values, Negate)
{
}
