using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Indexing;

namespace Umbraco.Cms.Search.Core.PropertyValueHandlers;

public sealed class DecimalPropertyValueHandler : IPropertyValueHandler
{
    public bool CanHandle(string propertyEditorAlias)
        => propertyEditorAlias is Cms.Core.Constants.PropertyEditors.Aliases.Decimal
            or Cms.Core.Constants.PropertyEditors.Aliases.PlainDecimal;

    public IndexValue? GetIndexValue(IContentBase content, IProperty property, string? culture, string? segment, bool published)
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