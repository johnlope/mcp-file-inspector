using System;

namespace McpFileInspector.Configurations;

public class CodeContextOptions
{
    public const string SectionName = "CodeContextOptions";

    public int MaxCalls { get; set; } = 100;
    public int MaxSeconds { get; set; } = 60;
    public long MaxBytes { get; set; } = 1_048_576; // 1 MB por defecto
    public int? MaxDegreeOfParallelism { get; set; } = 4;
    public int MaxFileSize { get; set; } = 102400; // 100 KB
    public int MaxFilesToList { get; set; } = 1000;
}
