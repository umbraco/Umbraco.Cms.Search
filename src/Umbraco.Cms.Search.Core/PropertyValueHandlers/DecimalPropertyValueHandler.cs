using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Indexing;

namespace Umbraco.Cms.Search.Core.PropertyValueHandlers;

public sealed class DecimalPropertyValueHandler : IPropertyValueHandler
{
    // TODO: include Umbraco.Plain.Decimal in V15
    public bool CanHandle(string propertyEditorAlias)
        => propertyEditorAlias is Constants.PropertyEditors.Aliases.Decimal;

    public IndexValue? GetIndexValue(IContent content, IProperty property, string? culture, string? segment, bool published)
    {
        var value = content.GetValue<decimal?>(property.Alias, culture, segment, published);
        return value.HasValue
            ? new IndexValue
            {
                Decimals = [value.Value]
            }
            : null;
    }
}