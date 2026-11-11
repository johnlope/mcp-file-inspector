using System.ComponentModel;
using ModelContextProtocol.Server;

public class RepositoryTools
{
    public RepositoryTools(PathValidator pathValidator, FileService fileService)
    {
        _pathValidator = pathValidator;
        _fileService = fileService;
    }

    private readonly PathValidator _pathValidator;
    private readonly FileService _fileService;

    [McpServerTool]
    [Description("Lists files and directories inside a repository path.")]
    public string ListFiles(
        [Description("Name of the repository to access")]
        string repositoryName)
    {
        try
        {
            string repositoryRoot = _pathValidator.GetRepositoryRoot(repositoryName);

            var fullPath = Path.GetFullPath(Path.Combine(repositoryRoot));

            if (!fullPath.StartsWith(Path.GetFullPath(repositoryRoot), StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Path is outside the repository.");
            }

            if (!Directory.Exists(fullPath))
                return "Directory not found.";

            return _fileService.GetFilteredFiles(fullPath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.ToString());
            throw;
        }
    }
}