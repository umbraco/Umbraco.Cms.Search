using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Indexing;

namespace Umbraco.Cms.Search.Core.PropertyValueHandlers;

public class BooleanPropertyValueHandler : IPropertyValueHandler, ICorePropertyValueHandler
{
    public bool CanHandle(string propertyEditorAlias)
        => propertyEditorAlias is Umbraco.Cms.Core.Constants.PropertyEditors.Aliases.Boolean;

    public IndexValue? GetIndexValue(IContentBase content, IProperty property, string? culture, string? segment, bool published)
    {
        var value = ParsePropertyValue(content, property, culture, segment, published);
        return value.HasValue
            ? new IndexValue
            {
                Integers = [value.Value]
            }
            : null;
    }

    private static int? ParsePropertyValue(IContentBase content, IProperty property, string? culture, string? segment, bool published)
    {
        var value = content.GetValue(property.Alias, culture, segment, published);
        return value switch
        {
            bool booleanValue => booleanValue ? 1 : 0,
            int integerValue => integerValue,
            _ => null
        };
    }
}