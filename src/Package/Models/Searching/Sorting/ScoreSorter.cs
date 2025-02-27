using Umbraco.Cms.Core;

namespace Package.Models.Searching.Sorting;

public record ScoreSorter(Direction Direction) : Sorter(IndexConstants.FieldNames.Score, Direction)
{
}