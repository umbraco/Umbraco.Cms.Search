using Umbraco.Cms.Core;

namespace Package.Models.Searching.Sorting;

public record StringSorter(string FieldName, Direction Direction) : Sorter(FieldName, Direction)
{
}