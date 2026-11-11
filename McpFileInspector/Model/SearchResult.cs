using System.Text.Json.Serialization;

namespace McpFileInspector.Model;

public class SearchResult
{
    [JsonPropertyName("file"), 
    JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? File { get; init; }
    [JsonPropertyName("line")]
    public int Line { get; init; }
    [JsonPropertyName("content"), 
    JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Content { get; init; }
}
