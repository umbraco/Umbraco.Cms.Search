using Umbraco.Cms.Core.Composing;

namespace Package.PropertyValueHandlers.Collection;

public sealed class PropertyValueHandlerCollection : BuilderCollectionBase<IPropertyValueHandler>
{
    public PropertyValueHandlerCollection(Func<IEnumerable<IPropertyValueHandler>> items)
        : base(items)
    {
    }
}