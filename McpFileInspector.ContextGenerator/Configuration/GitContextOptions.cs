using System;

namespace McpFileInspector.ContextGenerator.Configuration;

public class GitContextOptions
{
    public const string SectionName = "GitContext";
    public int ContextLines { get; internal set; }

    public string? WorkingDirectory { get; internal set; }
}
