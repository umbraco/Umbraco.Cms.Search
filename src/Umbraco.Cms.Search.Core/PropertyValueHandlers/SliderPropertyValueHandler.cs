using System.Globalization;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Indexing;

namespace Umbraco.Cms.Search.Core.PropertyValueHandlers;

public class SliderPropertyValueHandler : IPropertyValueHandler, ICorePropertyValueHandler
{
    public bool CanHandle(string propertyEditorAlias)
        => propertyEditorAlias is Umbraco.Cms.Core.Constants.PropertyEditors.Aliases.Slider;

    public IndexValue? GetIndexValue(IContentBase content, IProperty property, string? culture, string? segment, bool published)
    {
        var values = ParsePropertyValue(content, property, culture, segment, published);
        return values is not null
            ? new IndexValue
            {
                Decimals = values
            }
            : null;
    }

    private static decimal[]? ParsePropertyValue(IContentBase content, IProperty property, string? culture, string? segment, bool published)
    {
        var value = content.GetValue<string>(property.Alias, culture, segment, published);
        if (value is null)
        {
            return null;
        }

        var parts = value.Split(Cms.Core.Constants.CharArrays.Comma);
        var parsed = parts
            .Select(s => decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var i) ? i : (decimal?)null)
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .ToArray();

        return parsed.Length == parts.Length && parsed.Length <= 2
            ? parsed
            : null;
    }
}