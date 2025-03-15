using System.Globalization;
using Umbraco.Cms.Core.Models;
using Umbraco.Extensions;
using CoreConstants = Umbraco.Cms.Core.Constants;

namespace Umbraco.Cms.Search.Core.Extensions;

internal static class ContentExtensions
{
    // NOTE: this was duplicated from Umbraco core extensions, because they only work with IContent for some reason.
    public static IEnumerable<int> GetAncestorIds(this IContentBase content)
        => content.Path.Split(CoreConstants.CharArrays.Comma)
            .Where(x => x != CoreConstants.System.RootString && x != content.Id.ToString(CultureInfo.InvariantCulture))
            .Select(s => int.Parse(s, CultureInfo.InvariantCulture));

    public static string[] PublishedCultures(this IContentBase content)
        => content is IContent c && c.VariesByCulture()
            ? c.PublishedCultures.ToArray()
            : [null];

    public static string[] AvailableCultures(this IContentBase content)
        => content.VariesByCulture()
            ? content.AvailableCultures.ToArray()
            : [null];

    public static bool IsPublished(this IContentBase content)
        => content is IContent { Published: true };

    public static bool VariesByCulture(this IContentBase content)
        => content is IContent c && c.ContentType.VariesByCulture();

    public static UmbracoObjectTypes GetObjectType(this IContentBase content)
        => content switch
        {
            IContent => UmbracoObjectTypes.Document,
            IMedia => UmbracoObjectTypes.Media,
            IMember => UmbracoObjectTypes.Member,
            _ => throw new ArgumentOutOfRangeException(nameof(content))
        };
}