﻿using Umbraco.Cms.Core.Composing;

namespace Umbraco.Cms.Search.Core.PropertyValueHandlers.Collection;

public sealed class PropertyValueHandlerCollection : BuilderCollectionBase<IPropertyValueHandler>
{
    public PropertyValueHandlerCollection(Func<IEnumerable<IPropertyValueHandler>> items)
        : base(items)
    {
    }
}