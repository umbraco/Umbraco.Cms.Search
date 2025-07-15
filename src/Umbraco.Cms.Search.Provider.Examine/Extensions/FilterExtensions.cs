using Umbraco.Cms.Search.Core.Models.Searching.Filtering;

namespace Umbraco.Cms.Search.Provider.Examine.Extensions;

internal static class FilterExtensions
{
    internal static IntegerNonNullableRangeFilter ToNonNullableRangeFilter(this IntegerRangeFilter filter)
    {
        return new IntegerNonNullableRangeFilter(filter.FieldName, filter.Ranges.Select(x => new FilterRange<int>(x.MinimumValue ?? int.MinValue, x.MaximumValue ?? int.MaxValue)).ToArray(), filter.Negate);
    }
    
    internal static DecimalNonNullableRangeFilter ToNonNullableRangeFilter(this DecimalRangeFilter filter)
    {
        return new DecimalNonNullableRangeFilter(filter.FieldName, filter.Ranges.Select(x => new FilterRange<double>((double?)x.MinimumValue ?? double.MinValue, (double?)x.MaximumValue ?? double.MaxValue)).ToArray(), filter.Negate);
    }
    
    internal static DateTimeNonNullableRangeFilter ToNonNullableRangeFilter(this DateTimeOffsetRangeFilter filter)
    {
        return new DateTimeNonNullableRangeFilter(filter.FieldName, filter.Ranges.Select(x => new FilterRange<DateTime>(x.MinimumValue?.DateTime ?? DateTime.MinValue, x.MaximumValue?.DateTime ?? DateTime.MaxValue)).ToArray(), filter.Negate);
    }

    internal static DoubleExactFilter ToDoubleExactFilter(this DecimalExactFilter filter)
    {
        return new DoubleExactFilter(filter.FieldName, filter.Values.Select(x => (double) x).ToArray(), filter.Negate);
    }
    
    internal static DateTimeExactFilter ToDateTimeExactFilter(this DateTimeOffsetExactFilter filter)
    {
        return new DateTimeExactFilter(filter.FieldName, filter.Values.Select(x =>  x.DateTime).ToArray(), filter.Negate);
    }
}

internal record IntegerNonNullableRangeFilter(string FieldName, FilterRange<int>[] Ranges, bool Negate)
    : RangeFilter<int>(FieldName, Ranges, Negate)
{
}

internal record DecimalNonNullableRangeFilter(string FieldName, FilterRange<double>[] Ranges, bool Negate)
    : RangeFilter<double>(FieldName, Ranges, Negate)
{
}

internal record DateTimeNonNullableRangeFilter(string FieldName, FilterRange<DateTime>[] Ranges, bool Negate)
    : RangeFilter<DateTime>(FieldName, Ranges, Negate)
{
}

internal record DoubleExactFilter(string FieldName, double[] Values, bool Negate)
    : ExactFilter<double>(FieldName, Values, Negate)
{
}

internal record DateTimeExactFilter(string FieldName, DateTime[] Values, bool Negate)
    : ExactFilter<DateTime>(FieldName, Values, Negate)
{
}