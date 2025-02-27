namespace Package.Models.Indexing;

public record IndexValue
{
    public IEnumerable<string>? Texts { get; init; }

    public IEnumerable<string>? Keywords { get; init; }

    public IEnumerable<int>? Integers { get; init; }

    public IEnumerable<decimal>? Decimals { get; init; }

    public IEnumerable<DateTimeOffset>? DateTimeOffsets { get; init; }
}