namespace Package.Models.Indexing;

public record IndexField(string Alias, IndexValue Value, string? Culture, string? Segment);