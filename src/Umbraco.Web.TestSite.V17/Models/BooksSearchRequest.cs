namespace Site.Models;

public class BooksSearchRequest
{
    public string? Query { get; init; }

    public string[]? Length { get; init; }

    public string[]? AuthorNationality { get; init; }

    public string[]? PublishYear { get; init; }

    public string? SortBy { get; init; }

    public string? SortDirection { get; init; }

    public int Skip { get; init; } = 0;

    public int Take { get; init; } = 12;
}
