using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Indexing;

namespace Umbraco.Cms.Search.Core.PropertyValueHandlers;

public interface IPropertyValueHandler : IDiscoverable
{
    bool CanHandle(string propertyEditorAlias);

    IndexValue? GetIndexValue(IContent content, IProperty property, string? culture, string? segment, bool published);
}