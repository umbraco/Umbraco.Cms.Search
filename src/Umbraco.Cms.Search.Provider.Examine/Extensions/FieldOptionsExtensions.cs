using Umbraco.Cms.Search.Provider.Examine.Configuration;

namespace Umbraco.Cms.Search.Provider.Examine.Extensions;

internal static class FieldOptionsExtensions
{
    public static bool HasKeywordField(this FieldOptions fieldOptions, string fieldName)
        => fieldOptions.Fields.Any(f => f.FieldValues == FieldValues.Keywords && f.PropertyName == fieldName);
}
