using System;
using System.Text.Json.Serialization;

namespace McpFileInspector.ContextGenerator.Models;

public class ReviewManifest
{

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    [JsonPropertyName("generatedAt")]
    public DateTime GeneratedAt { get; init; }

    [JsonPropertyName("changeSet")]
    public ChangeSet ChangeSet { get; init; } = null!;

    [JsonPropertyName("summary")]
    public Summary Summary { get; init; } = null!;

    [JsonPropertyName("files")]
    public IReadOnlyList<GitFile> Files { get; init; } = [];
}

// Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);
public class ChangeSet
{
    [JsonPropertyName("team")]
    public string Team { get; set; }

    [JsonPropertyName("repository")]
    public string Repository { get; set; }

    [JsonPropertyName("baseCommit")]
    public string BaseCommit { get; init; }

    [JsonPropertyName("targetCommit")]
    public string TargetCommit { get; init; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("commits")]
    public IReadOnlyList<string> Commits { get; init; } = [];
}

public class Summary
{
    [JsonPropertyName("totalFilesChanged")]
    public int TotalFilesChanged { get; init; }

    [JsonPropertyName("totalAdditions")]
    public int TotalAdditions { get; init; }

    [JsonPropertyName("totalDeletions")]
    public int TotalDeletions { get; init; }

    [JsonPropertyName("modulesChanged")]
    public IReadOnlyList<string> ModulesChanged { get; init; } = [];
}




