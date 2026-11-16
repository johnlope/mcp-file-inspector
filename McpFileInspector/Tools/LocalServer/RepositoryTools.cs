using System.ComponentModel;
using McpFileInspector.Model;
using McpFileInspector.Services;
using ModelContextProtocol.Server;

public class RepositoryTools 
{
    private readonly IPathValidator _pathValidator;
    private readonly IFileService _fileService;

    public RepositoryTools(IPathValidator pathValidator, IFileService fileService)
    {
        _pathValidator = pathValidator;
        _fileService = fileService;
    }

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
            return McpResponse.Success(files);
        }
        catch (Exception ex)
        {
            return McpResponse.Fail<IReadOnlyList<string>>(ex);
        }
    }

    [McpServerTool]
    [Description("Reads the content of a file inside a local repository.")]
    public async Task<McpToolResponse<SearchResult>> ReadFile(
        [Description("Name of the repository to access")]
        string repositoryName,
        [Description("Relative path of the file to read")]
        string relativeFilePath,
        [Description("Optional 1-based start line number")]
        int? startLine = null,
        [Description("Optional 1-based end line number")]
        int? endLine = null)
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

            SearchResult result = startLine.HasValue || endLine.HasValue
                ? await _fileService.ReadFileAsync(fullPath, startLine, endLine)
                : await _fileService.ReadFileAsync(fullPath);

            return McpResponse.Success(result);
        }
        catch (Exception ex)
        {
            return McpResponse.Fail<SearchResult>(ex);
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
            return McpResponse.Success(files);
        }
        catch (Exception ex)
        {
            return McpResponse.Fail<IReadOnlyList<string>>(ex);
        }
    }

    [McpServerTool]
    [Description("Searches for content inside files within a local repository.")]
    public async Task<McpToolResponse<IReadOnlyList<SearchResult>>> SearchContentAsync(
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

            IReadOnlyList<SearchResult> files = await _fileService.SearchContentAsync(repositoryRoot, searchPattern, content, mode: modeCast);
            return McpResponse.Success(files);
        }
        catch (Exception ex)
        {
            return McpResponse.Fail<IReadOnlyList<SearchResult>>(ex);
        }
    }
}