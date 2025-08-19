namespace Umbraco.Cms.Search.Provider.Examine.Extensions;

internal static class StringExtensions
{
    internal static string TransformDashes(this string value)
        => value.Replace('-', '_');

    internal static string KeywordFieldName(this string fieldName)
        => $"{fieldName}{Constants.Fields.KeywordFieldPostfix}";
}
