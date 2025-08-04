namespace Umbraco.Cms.Search.Provider.Examine.Extensions;

internal static class StringExtensions
{
    internal static string TransformDashes(this string value)
    {
        return value.Replace('-', '_');
    }
}