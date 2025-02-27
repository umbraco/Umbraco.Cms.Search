using Umbraco.Cms.Core.Composing;

namespace Package.PropertyValueHandlers.Collection;

public sealed class PropertyValueHandlerCollectionBuilder
    : LazyCollectionBuilderBase<PropertyValueHandlerCollectionBuilder, PropertyValueHandlerCollection, IPropertyValueHandler>
{
    protected override PropertyValueHandlerCollectionBuilder This => this;
}
