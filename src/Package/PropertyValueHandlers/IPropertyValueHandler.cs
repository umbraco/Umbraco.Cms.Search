using Package.Models.Indexing;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Models;

namespace Package.PropertyValueHandlers;

public interface IPropertyValueHandler : IDiscoverable
{
    bool CanHandle(string propertyEditorAlias);

    IndexValue? GetIndexValue(IContent content, IProperty property, string? culture, string? segment, bool published);
}