using Umbraco.Cms.Search.Core.Models.Indexing;

namespace Umbraco.Cms.Search.Provider.Examine.Helpers;

internal static class FieldNameHelper
{
    public static string FieldName(IndexField field, string fieldValues)
        => FieldName(field.FieldName, fieldValues);

    public static string FieldName(string fieldName, string fieldValues)
        => $"Field_{fieldName}_{fieldValues}";
}
