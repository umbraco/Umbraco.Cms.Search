namespace Umbraco.Cms.Search.Core.Models.Searching.Filtering;

public record FilterRange<T>(T? Min, T? Max)
{
}