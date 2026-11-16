using System;
using McpFileInspector.Model;

namespace McpFileInspector.Services;

public interface IFileService
{
    IReadOnlyList<string> GetFilteredFiles(string requestedPath);
    IReadOnlyList<string> SearchFiles(string repositoryRoot, string searchPattern);
    Task<IReadOnlyList<SearchResult>> SearchContentAsync(string repositoryRoot, string searchPattern, string searchTerm, SearchMode mode = SearchMode.Text);
    Task<SearchResult> ReadFileAsync(string fullPath, CancellationToken cancellationToken);
    Task<SearchResult> ReadFileAsync(string fullPath, int? startLine = null, int? endLine = null, CancellationToken cancellationToken = default);
    Task<FileOutlineResult> GetFileOutlineAsync(string fullPath, CancellationToken cancellationToken = default);
    SessionSummary GetSessionSummary();
}
