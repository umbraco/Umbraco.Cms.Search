using Umbraco.Cms.Core;

namespace Package.Models.Searching.Sorting;

public record DateTimeOffsetSorter(string FieldName, Direction Direction) : Sorter(FieldName, Direction)
{
}