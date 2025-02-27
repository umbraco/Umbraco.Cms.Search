using Umbraco.Cms.Core;

namespace Package.Models.Searching.Sorting;

public record IntegerSorter(string FieldName, Direction Direction) : Sorter(FieldName, Direction)
{
}