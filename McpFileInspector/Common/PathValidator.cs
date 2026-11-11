using Microsoft.Extensions.Configuration;

public class PathValidator
{
    private readonly Dictionary<string, string> _allowedRoots;   

    public PathValidator(IConfiguration configuration)
    {
        _allowedRoots = configuration.GetSection("McpSettings:AllowedRoots")
            .Get<Dictionary<string, string>>() ?? new(StringComparer.OrdinalIgnoreCase);
    }

    public string GetRepositoryRoot(string repositoryName)
    {
        if(string.IsNullOrEmpty(repositoryName))
            throw new ArgumentException("Repository name cannot be null or empty.", nameof(repositoryName));        
        
        if(!_allowedRoots.ContainsKey(repositoryName))
            throw new UnauthorizedAccessException($"Repository not allowed: {repositoryName}");

        return _allowedRoots[repositoryName];
    }
}