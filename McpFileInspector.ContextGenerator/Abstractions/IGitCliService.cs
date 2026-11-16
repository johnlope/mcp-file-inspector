
using McpFileInspector.ContextGenerator.Models;

namespace McpFileInspector.ContextGenerator.Abstractions;

public interface IGitCliService
{
	Task<IReadOnlyList<string>> LogCommitsAsync(string baseCommit, string targetCommit, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<string>> RevListAsync(string baseCommit, string targetCommit, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<GitFile>> DiffNumStatAsync(string baseCommit, string targetCommit, CancellationToken cancellationToken = default);
	Task<string> DiffFilePatchAsync(string baseCommit, string targetCommit, string filePath, CancellationToken cancellationToken = default);
	Task<GitCommit?> LogSummaryAsync(string commitSha, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<string>> DiffNameOnlyAsync(string baseCommit, string targetCommit, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<GitFile>> DiffNameStatusAsync(string baseCommit, string targetCommit, CancellationToken cancellationToken = default);
	
}
