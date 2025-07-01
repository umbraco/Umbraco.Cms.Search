namespace Umbraco.Cms.Search.Core.Extensions;

public static class ArrayExtensions
{
    internal static T[]? NullIfEmpty<T>(this T[] source)
        => source.Length > 0 ? source : null;
}