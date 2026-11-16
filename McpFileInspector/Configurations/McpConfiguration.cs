using System;

namespace McpFileInspector.Configurations;

public class McpConfiguration
{
    public const string SectionName = "McpConfiguration";

    public Dictionary<string, string> AllowedRoots { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> AllowedExtensions { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> ExcludedDirectories { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> ExcludedExtensions { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> ExcludedFiles { get; set; } = new(StringComparer.OrdinalIgnoreCase);   
}
