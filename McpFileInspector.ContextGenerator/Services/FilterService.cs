using System;
using System.IO.Enumeration;
using McpFileInspector.ContextGenerator.Abstractions;
using McpFileInspector.ContextGenerator.Configuration;

namespace McpFileInspector.ContextGenerator.Services;

public class FilterService : IFilterService
{
    public static IReadOnlyList<string>? filteredPatterns;
    private readonly ContextGeneratorOptions options;

    public FilterService(ContextGeneratorOptions options)
    {
        this.options = options;
    }

    private async Task<IReadOnlyList<string>?> LoadFilesAsync(string fullPath)
    {
        if (filteredPatterns != null)
            return filteredPatterns;

        if (fullPath == null)
            throw new ArgumentNullException(nameof(fullPath));

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"The file '{fullPath}' does not exist.");

        await using var stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite,
            bufferSize: 4096,
            useAsync: true);

        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();
        filteredPatterns = content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        return filteredPatterns;
    }

    public async Task<bool> FilterAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var patterns = await LoadFilesAsync(options.ExludedFilesPath) ?? [];
        var fileName = Path.GetFileName(filePath);

        if (patterns.Count == 0)
            return true;

        return !patterns.Any(pattern => FileSystemName.MatchesSimpleExpression(
                    pattern,
                    fileName,
                    ignoreCase: true)
        );
    }
}
