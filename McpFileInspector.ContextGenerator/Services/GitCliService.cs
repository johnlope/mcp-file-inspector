using McpFileInspector.ContextGenerator.Abstractions;
using McpFileInspector.ContextGenerator.Configuration;
using McpFileInspector.ContextGenerator.Models;

namespace McpFileInspector.ContextGenerator.Services;

public class GitCliService : IGitService
{
    private readonly GitContextOptions gitContextOptions;
    private readonly GitProcessRunner processRunner;

    public GitCliService(GitContextOptions gitContextOptions)
    {
        if (gitContextOptions.WorkingDirectory is null)
            throw new ArgumentNullException(nameof(gitContextOptions.WorkingDirectory));

        this.gitContextOptions = gitContextOptions;
        this.processRunner = new GitProcessRunner(gitContextOptions.WorkingDirectory);
    }

    /// <summary>
    /// Retrieves the list of commit hashes between the specified base and target commits.
    /// </summary>
    /// <param name="baseCommit"></param>
    /// <param name="targetCommit"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<string>> LogCommitsAsync(string baseCommit, string targetCommit, CancellationToken cancellationToken = default)
    {
        string logCommitsCommand = GitCommands.LogCommits(baseCommit, targetCommit);
        string commitsRaw = await processRunner.ExecuteAsync(logCommitsCommand, cancellationToken);
        return commitsRaw?.Split(['\n'], StringSplitOptions.RemoveEmptyEntries)?.Select(c => c.Trim()).ToArray() ?? Array.Empty<string>();
    }

    /// <summary>
    /// Retrieves the list of files changed between the specified base and target commits.
    /// </summary>
    /// <param name="baseCommit"></param>
    /// <param name="targetCommit"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<string>> DiffNameOnlyAsync(string baseCommit, string targetCommit, CancellationToken cancellationToken = default)
    {
        string diffNameOnlyCommand = GitCommands.DiffNameOnly(baseCommit, targetCommit);
        string filesRaw = await processRunner.ExecuteAsync(diffNameOnlyCommand, cancellationToken);
        return filesRaw?.Split(['\n'], StringSplitOptions.RemoveEmptyEntries)?.Select(f => f.Trim()).ToArray() ?? Array.Empty<string>();
    }

    /// <summary>
    /// Retrieves the list of files with their status between the specified base and target commits.
    /// </summary>
    /// <param name="baseCommit"></param>
    /// <param name="targetCommit"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<GitFile>> DiffNameStatusAsync(string baseCommit, string targetCommit, CancellationToken cancellationToken = default)
    {
        string diffNameStatusCommand = GitCommands.DiffNameStatus(baseCommit, targetCommit);
        string filesStatusRaw = await processRunner.ExecuteAsync(diffNameStatusCommand, cancellationToken);

        // Parse the raw output into FilesStatus objects
        var filesStatusList = new List<GitFile>();
        foreach (var line in filesStatusRaw?.Split(['\n'], StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>())
        {
            var parts = line.Trim().Split('\t');
            if (parts.Length >= 2)
            {
                var statusCode = parts[0];
                var filePath = parts.Length >= 3 ? parts[2] : parts[1]; // En renombrados (R100 \t old \t new) tomamos la nueva ruta

                filesStatusList.Add(new GitFile
                {
                    Status = ParseGitStatus(statusCode),
                    Path = filePath
                });
            }
        }

        return filesStatusList;
    }

    /// <summary>
    /// Retrieves the list of files with their additions and deletions between the specified base and target commits.
    /// </summary>
    /// <param name="baseCommit"></param>
    /// <param name="targetCommit"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<GitFile>> DiffNumStatAsync(string baseCommit, string targetCommit, CancellationToken cancellationToken = default)
    {
        string diffNumStatCommand = GitCommands.DiffNumStat(baseCommit, targetCommit);
        string filesStatusRaw = await processRunner.ExecuteAsync(diffNumStatCommand, cancellationToken);

        var filesStatusList = new List<GitFile>();
        foreach (var line in filesStatusRaw?.Split(['\n'], StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>())
        {
            var parts = line.Trim().Split('\t');
            if (parts.Length >= 3)
            {
                filesStatusList.Add(new GitFile
                {
                    Additions = int.TryParse(parts[0], out var additions) ? additions : null,
                    Deletions = int.TryParse(parts[1], out var deletions) ? deletions : null,
                    Path = parts[2]
                });
            }
        }

        return filesStatusList;
    }

    /// <summary>
    /// Retrieves the raw patch text for a specific file between the specified base and target commits.
    /// </summary>
    /// <param name="baseCommit"></param>
    /// <param name="targetCommit"></param>
    /// <param name="filePath"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<string> DiffFilePatchAsync(string baseCommit, string targetCommit, string filePath, CancellationToken cancellationToken = default)
    {
        string diffFilePatchCommand = GitCommands.DiffFilePatch(baseCommit, targetCommit, filePath, gitContextOptions.ContextLines);
        string patchRaw = await processRunner.ExecuteAsync(diffFilePatchCommand, cancellationToken);
        return patchRaw ?? string.Empty;
    }

    /// <summary>
    /// Retrieves a summary of a commit by its SHA.
    /// </summary>
    /// <param name="commitSha"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<GitCommit?> LogSummaryAsync(string commitSha, CancellationToken cancellationToken = default)
    {
        string logSummaryCommand = GitCommands.LogSummary(commitSha);
        string logSummaryRaw = await processRunner.ExecuteAsync(logSummaryCommand, cancellationToken);

        var line = logSummaryRaw?.Split(['\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(line)) return null;

        var parts = line.Split('|');
        if (parts.Length >= 4)
        {
            return new GitCommit
            {
                Sha = parts[0].Trim(),
                Author = parts[1].Trim(),
                Email = parts[2].Trim(),
                Message = parts[3].Trim()
            };
        }

        return null;
    }

    /// <summary>
    /// Retrieves a list of commit SHAs between the specified base and target commits.
    /// </summary>
    /// <param name="baseCommit"></param>
    /// <param name="targetCommit"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<string>> RevListAsync(string baseCommit, string targetCommit, CancellationToken cancellationToken = default)
    {
        string revListCommand = GitCommands.RevList(baseCommit, targetCommit);
        string revListRaw = await processRunner.ExecuteAsync(revListCommand, cancellationToken);

        var revList = new List<string>();
        foreach (var line in revListRaw?.Split(['\n'], StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>())
        {
            revList.Add(line.Trim());
        }

        return revList;
    }

    private static string ParseGitStatus(string? gitStatus)
    {
        if (string.IsNullOrWhiteSpace(gitStatus)) return "Unknown";

        var status = gitStatus.Trim().ToUpperInvariant();
        if (status.StartsWith('R')) return "Renamed";
        if (status.StartsWith('C')) return "Copied";

        return status switch
        {
            "A" => "Added",
            "M" => "Modified",
            "D" => "Deleted",
            "U" => "Updated",
            "??" => "Untracked",
            _ => "Unknown"
        };
    }
}
