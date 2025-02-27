using Umbraco.Cms.Core;

namespace Package.Models.Searching.Sorting;

public abstract record Sorter(string FieldName, Direction Direction)
{
}