using Umbraco.Cms.Search.Core.Models.Indexing;

namespace Umbraco.Cms.Search.Provider.Examine.Helpers;

internal static class FieldNameHelper
{
    public static string FieldName(IndexField field, string fieldValues)
        => FieldName(field.FieldName, fieldValues, field.Segment);

    public static string FieldName(string fieldName, string fieldValues)
        => $"Field_{fieldName}_{fieldValues}";

    public static string FieldName(string fieldName, string fieldValues, string? segment)
    {
        var result = $"Field_{fieldName}_{fieldValues}";

        if (segment is not null)
        {
            result += $"_{segment}";
        }

        return result;
    }

    public static string QueryableKeywordFieldName(string fieldName)
        => $"__Query_{fieldName}";

    public static string SegmentedSystemFieldName(string systemFieldName, string? segment)
        => segment is null ? systemFieldName : $"{systemFieldName}_{segment}";
}
