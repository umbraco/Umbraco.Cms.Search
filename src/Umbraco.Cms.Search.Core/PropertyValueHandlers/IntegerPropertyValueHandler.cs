using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Indexing;

namespace Umbraco.Cms.Search.Core.PropertyValueHandlers;

public sealed class IntegerPropertyValueHandler : IPropertyValueHandler, ICorePropertyValueHandler
{
    public bool CanHandle(string propertyEditorAlias)
        => propertyEditorAlias is Cms.Core.Constants.PropertyEditors.Aliases.Integer
            or Cms.Core.Constants.PropertyEditors.Aliases.PlainInteger;

    public IndexValue? GetIndexValue(IContentBase content, IProperty property, string? culture, string? segment, bool published)
    {
        var value = content.GetValue<int?>(property.Alias, culture, segment, published);
        return value.HasValue
            ? new IndexValue
            {
                Integers = [value.Value]
            }
            : null;
    }
}