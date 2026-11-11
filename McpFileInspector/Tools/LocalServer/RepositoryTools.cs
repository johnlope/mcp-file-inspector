using System.ComponentModel;
using McpFileInspector.Model;
using ModelContextProtocol.Server;

public class RepositoryTools : FileInspectorBaseTools
{
    public RepositoryTools(PathValidator pathValidator, FileService fileService)
    {
        _pathValidator = pathValidator;
        _fileService = fileService;
    }

    private readonly PathValidator _pathValidator;
    private readonly FileService _fileService;

    [McpServerTool]
    [Description("Lists files and directories inside a local repository path.")]
    public McpToolResponse<IReadOnlyList<string>> ListFiles(
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
                throw new DirectoryNotFoundException("Directory not found.");

            IReadOnlyList<string> files = _fileService.GetFilteredFiles(fullPath);
            return ProcessSuccessResponse(files);
        }
        catch (Exception ex)
        {
            return ProcessFailedResponse<IReadOnlyList<string>>(ex);
        }
    }

    [McpServerTool]
    [Description("Reads the content of a file inside a local repository.")]
    public McpToolResponse<string> ReadFile(
        [Description("Name of the repository to access")]
        string repositoryName,
        [Description("Relative path of the file to read")]
        string relativeFilePath)
    {
        try
        {
            string repositoryRoot = _pathValidator.GetRepositoryRoot(repositoryName);

            var fullPath = Path.GetFullPath(Path.Combine(repositoryRoot, relativeFilePath));

            if (!fullPath.StartsWith(Path.GetFullPath(repositoryRoot), StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Path is outside the repository.");
            }

            if (!File.Exists(fullPath))
                throw new FileNotFoundException("File not found.");
            string content = _fileService.ReadFile(fullPath);
            return ProcessSuccessResponse(content);

        }
        catch (Exception ex)
        {
            return ProcessFailedResponse<string>(ex);
        }
    }

    [McpServerTool]
    [Description("Searches for files inside a local repository.")]
    public McpToolResponse<IReadOnlyList<string>> SearchFiles(
        [Description("Name of the repository to access")]
        string repositoryName,
        [Description("Search pattern for files (e.g., *.txt, Program.cs, *Service.cs, Program.*)")]
        string searchPattern
    )
    {
        try
        {
            string repositoryRoot = _pathValidator.GetRepositoryRoot(repositoryName);

            var fullPath = Path.GetFullPath(Path.Combine(repositoryRoot, searchPattern));

            if (!fullPath.StartsWith(Path.GetFullPath(repositoryRoot), StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Path is outside the repository.");
            }

            if (!Directory.Exists(repositoryRoot))
                throw new DirectoryNotFoundException("Directory not found.");

            IReadOnlyList<string> files = _fileService.SearchFiles(repositoryRoot, searchPattern);
            return ProcessSuccessResponse(files);
        }
        catch (Exception ex)
        {
            return ProcessFailedResponse<IReadOnlyList<string>>(ex);
        }
    }

    [McpServerTool]
    [Description("Searches for content inside files within a local repository.")]
    public async Task<McpToolResponse<IReadOnlyList<string>>> SearchContentAsync(
        [Description("Name of the repository to access")]
        string repositoryName,
        [Description("Search pattern for files (e.g., *.txt, Program.cs, *Service.cs, Program.*)")]
        string searchPattern,
        [Description("Text or regular expression pattern to search for inside file contents, depending on the selected search mode.")]
        string content,
        [Description("Search mode (e.g., 'Text', 'Regex')")]
        string mode
    )
    {
        try
        {
            string repositoryRoot = _pathValidator.GetRepositoryRoot(repositoryName);

            var fullPath = Path.GetFullPath(Path.Combine(repositoryRoot, searchPattern));

            if (!fullPath.StartsWith(Path.GetFullPath(repositoryRoot), StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Path is outside the repository.");
            }

            if (Enum.TryParse<SearchMode>(mode, true, out var modeCast) == false)
                throw new ArgumentException("Invalid search mode.");            

            if (!Directory.Exists(repositoryRoot))
                throw new DirectoryNotFoundException("Directory not found.");

            IReadOnlyList<string> files = await _fileService.SearchContentAsync(repositoryRoot, searchPattern, content, mode: modeCast);
            return ProcessSuccessResponse(files);
        }
        catch (Exception ex)
        {
            return ProcessFailedResponse<IReadOnlyList<string>>(ex);
        }
    }
}