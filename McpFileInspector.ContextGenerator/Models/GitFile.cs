using System.Text.Json.Serialization;

namespace McpFileInspector.ContextGenerator.Models;

public class GitFile
{
    [JsonPropertyName("path"), 
    JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Path { get; set; }

    [JsonPropertyName("status"), 
    JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Status { get; set; }

    [JsonPropertyName("module"), 
    JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Module { get; set; }

    [JsonPropertyName("language"), 
    JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Language { get; set; }

    [JsonPropertyName("additions"), 
    JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Additions { get; set; }

    [JsonPropertyName("deletions"), 
    JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Deletions { get; set; }

    [JsonPropertyName("patch"), 
    JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Patch { get; set; }

}
