namespace Umbraco.Cms.Search.Core.Models.Searching.Filtering;

public record FilterRange<T>(T MinimumValue, T MaximumValue)
{
}