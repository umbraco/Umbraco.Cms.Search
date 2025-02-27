using Umbraco.Cms.Core;

namespace Package.Models.Searching.Sorting;

public record KeywordSorter(string FieldName, Direction Direction) : Sorter(FieldName, Direction)
{
}