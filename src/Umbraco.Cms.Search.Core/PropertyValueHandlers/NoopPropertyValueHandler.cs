using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Indexing;

namespace Umbraco.Cms.Search.Core.PropertyValueHandlers;

public sealed class NoopPropertyValueHandler : IPropertyValueHandler, ICorePropertyValueHandler
{
    public bool CanHandle(string propertyEditorAlias)
        => propertyEditorAlias is Cms.Core.Constants.PropertyEditors.Aliases.EmailAddress
            or Cms.Core.Constants.PropertyEditors.Aliases.ColorPicker
            or Cms.Core.Constants.PropertyEditors.Aliases.ColorPickerEyeDropper;
    public IndexValue? GetIndexValue(IContentBase content, IProperty property, string? culture, string? segment, bool published)
        => null;
}