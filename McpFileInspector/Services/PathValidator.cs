using McpFileInspector.Configurations;

namespace McpFileInspector.Services;

public class PathValidator : IPathValidator
{
    private readonly Dictionary<string, string> _allowedRoots;

    public PathValidator(McpConfiguration configuration)
    {
        _allowedRoots = configuration.AllowedRoots ?? new(StringComparer.OrdinalIgnoreCase);
    }

    public string GetRepositoryRoot(string repositoryName)
    {
        if (string.IsNullOrEmpty(repositoryName))
            throw new ArgumentException("Repository name cannot be null or empty.", nameof(repositoryName));

        if (_allowedRoots == null || _allowedRoots.Count == 0)
            throw new UnauthorizedAccessException("No allowed repositories are configured.");

        if (!_allowedRoots.ContainsKey(repositoryName))
            throw new UnauthorizedAccessException($"Repository not allowed: {repositoryName}");

        return _allowedRoots[repositoryName];
    }

    public string ValidateAndResolvePath(string repositoryName, string relativePath)
    {
        var repositoryRoot = Path.GetFullPath(GetRepositoryRoot(repositoryName));
        var fullPath = Path.GetFullPath(Path.Combine(repositoryRoot, relativePath ?? string.Empty));

        // Validar escape de directorio
        if (!fullPath.StartsWith(repositoryRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Path is outside the repository boundaries.");
        }

        if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
            throw new FileNotFoundException($"Path not found: {fullPath}");

        return fullPath;
    }
}