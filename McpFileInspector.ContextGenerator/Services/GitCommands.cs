namespace McpFileInspector.ContextGenerator.Services;

public static class GitCommands
{
    public static string DiffNameOnly(string baseCommit, string targetCommit) =>
        $"--no-pager diff --no-ext-diff --no-color --name-only {baseCommit} {targetCommit}";

    public static string DiffNameStatus(string baseCommit, string targetCommit) =>
        $"--no-pager diff --no-ext-diff --no-color --name-status {baseCommit} {targetCommit}";

    public static string DiffNumStat(string baseCommit, string targetCommit) =>
        $"--no-pager diff --no-ext-diff --no-color --numstat {baseCommit} {targetCommit}";

    public static string DiffFilePatch(string baseCommit, string targetCommit, string filePath, int contextLines = 60) =>
        $"--no-pager diff --no-ext-diff --no-color -U{contextLines} {baseCommit} {targetCommit} -- \"{filePath}\"";

    public static string LogSummary(string commitSha) =>
        $"--no-pager log -1 --no-color --format=\"%h|%an|%ae|%s\" {commitSha}";

    public static string RevList(string baseCommit, string targetCommit) =>
        $"--no-pager rev-list --no-merges {baseCommit}..{targetCommit}";

    public static string LogCommits(string baseCommit, string targetCommit) =>
        $"--no-pager log --no-merges --no-color --format=\"%H|%an|%s\" {baseCommit}..{targetCommit}";
}
