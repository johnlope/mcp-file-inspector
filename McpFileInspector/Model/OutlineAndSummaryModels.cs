using System.Text.Json.Serialization;

namespace McpFileInspector.Model;

public sealed record FileSymbol(
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("line")] int Line,
    [property: JsonPropertyName("signature"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Signature = null
);

public sealed record FileOutlineResult(
    [property: JsonPropertyName("file")] string File,
    [property: JsonPropertyName("totalLines")] int TotalLines,
    [property: JsonPropertyName("symbols")] IReadOnlyList<FileSymbol> Symbols
);

public sealed record SessionSummary(
    [property: JsonPropertyName("totalCalls")] int TotalCalls,
    [property: JsonPropertyName("totalBytesRead")] long TotalBytesRead,
    [property: JsonPropertyName("elapsedSeconds")] double ElapsedSeconds,
    [property: JsonPropertyName("queries")] IReadOnlyList<McpFileInspector.Abstractions.CodeQueryRecord> Queries
);
