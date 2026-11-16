namespace McpFileInspector.Services;

public interface IPathValidator
{
    string GetRepositoryRoot(string repositoryName);
    string ValidateAndResolvePath(string repositoryName, string relativePath);
}
