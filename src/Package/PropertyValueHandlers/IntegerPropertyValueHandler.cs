using Package.Models.Indexing;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;

namespace Package.PropertyValueHandlers;

public sealed class IntegerPropertyValueHandler : IPropertyValueHandler
{
    // TODO: include Umbraco.Plain.Integer in V15 
    public bool CanHandle(string propertyEditorAlias)
        => propertyEditorAlias is Constants.PropertyEditors.Aliases.Integer;

    public IndexValue? GetIndexValue(IContent content, IProperty property, string? culture, string? segment, bool published)
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