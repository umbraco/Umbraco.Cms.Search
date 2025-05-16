using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Indexing;

namespace Umbraco.Cms.Search.Core.PropertyValueHandlers;

public sealed class MultipleTextstringPropertyValueHandler : IPropertyValueHandler, ICorePropertyValueHandler
{
    public bool CanHandle(string propertyEditorAlias)
        => propertyEditorAlias is Umbraco.Cms.Core.Constants.PropertyEditors.Aliases.MultipleTextstring;

    public IndexValue? GetIndexValue(IContentBase content, IProperty property, string? culture, string? segment, bool published)
    {
        var values = ParsePropertyValue(content, property, culture, segment, published);
        return values?.Any() is true
            ? new IndexValue
            {
                Texts = values
            }
            : null;
    }

    private static string[]? ParsePropertyValue(IContentBase content, IProperty property, string? culture, string? segment, bool published)
    {
        var values = content
            .GetValue<string>(property.Alias, culture, segment, published)?
            .Split("\n");
        return values;
    }
}