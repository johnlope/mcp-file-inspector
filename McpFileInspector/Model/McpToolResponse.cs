using System.Text.Json.Serialization;

namespace McpFileInspector.Model;

public class McpToolResponse<T>
{
    public bool Success { get; init; }

    [JsonPropertyName("results"), 
    JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Results { get; init; }

    [JsonPropertyName("error"), 
    JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ResponseError? Error { get; init; }
}

public class ResponseError
{
    public string? Message { get; init; }
    public string? Code { get; init; }
}
