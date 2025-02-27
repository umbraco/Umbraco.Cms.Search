using Umbraco.Cms.Core;

namespace Package.Models.Searching.Sorting;

public record DecimalSorter(string FieldName, Direction Direction) : Sorter(FieldName, Direction)
{
}