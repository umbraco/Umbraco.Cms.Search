using Package.Models.Indexing;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Extensions;

namespace Package.PropertyValueHandlers;

public sealed class PlainStringPropertyValueHandler : IPropertyValueHandler
{
    // TODO: include Umbraco.Plain.String in V15 
    public bool CanHandle(string propertyEditorAlias)
        => propertyEditorAlias is Constants.PropertyEditors.Aliases.TextBox or Constants.PropertyEditors.Aliases.TextArea;

    public IndexValue? GetIndexValue(IContent content, IProperty property, string? culture, string? segment, bool published)
    {
        var value = content.GetValue<string>(property.Alias, culture, segment, published);
        return value.IsNullOrWhiteSpace()
            ? null
            : new IndexValue
            {
                Texts = [value]
            };
    }
}