using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Extensions;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Extensions;

namespace Umbraco.Cms.Search.Core.PropertyValueHandlers;

public sealed class ContentPickerPropertyValueHandler : IPropertyValueHandler
{
    public bool CanHandle(string propertyEditorAlias)
        => propertyEditorAlias is Umbraco.Cms.Core.Constants.PropertyEditors.Aliases.ContentPicker;

    public IndexValue? GetIndexValue(IContentBase content, IProperty property, string? culture, string? segment, bool published)
    {
        var key = ParsePropertyValue(content, property, culture, segment, published);
        return key.HasValue
            ? new IndexValue
            {
                Keywords = [key.Value.AsKeyword()]
            }
            : null;
    }

    private static Guid? ParsePropertyValue(IContentBase content, IProperty property, string? culture, string? segment, bool published)
    {
        var value = content.GetValue<string>(property.Alias, culture, segment, published);
        if (value.IsNullOrWhiteSpace()
            || UdiParser.TryParse(value, out var udi) is false
            || udi is not GuidUdi guidUdi)
        {
            return null;
        }

        return guidUdi.Guid; 
    }
}