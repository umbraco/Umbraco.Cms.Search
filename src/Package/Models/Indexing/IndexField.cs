namespace Package.Models.Indexing;

public record IndexField(string FieldName, IndexValue Value, string? Culture, string? Segment);