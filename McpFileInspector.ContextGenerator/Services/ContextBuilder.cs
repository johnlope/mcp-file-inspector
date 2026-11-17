using McpFileInspector.ContextGenerator.Abstractions;
using McpFileInspector.ContextGenerator.Models;

namespace McpFileInspector.ContextGenerator.Services;

public class ContextBuilder : IContextBuilder
{
    private readonly IGitService gitService;
    private readonly IFilterService filterService;

    public ContextBuilder(IGitService gitService, IFilterService filterService)
    {
        this.gitService = gitService;
        this.filterService = filterService;
    }

    public async Task<ReviewManifest> BuildReviewManifestAsync(string baseCommit, string targetCommit, ReviewSpecialty specialty = ReviewSpecialty.All, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> commits = await gitService.LogCommitsAsync(baseCommit, targetCommit, cancellationToken);
        IReadOnlyList<GitFile> filesChanged = await gitService.DiffNameStatusAsync(baseCommit, targetCommit, cancellationToken);
        IReadOnlyList<GitFile> changes = await gitService.DiffNumStatAsync(baseCommit, targetCommit, cancellationToken);

        if (changes == null || filesChanged == null) throw new InvalidOperationException("Failed to retrieve changes from git.");

        var changesByPath = changes
         .Where(c => !string.IsNullOrWhiteSpace(c.Path))
         .ToDictionary(
             c => c.Path!,
             c => c,
             StringComparer.OrdinalIgnoreCase
         );

        IReadOnlyList<GitFile> filteredFiles = await filesChanged
            .ToAsyncEnumerable()
            .Where(file => !string.IsNullOrWhiteSpace(file.Path))
            .Where(async (file, cancellationToken) => await filterService.FilterAsync(file.Path!, cancellationToken))
            .ToListAsync();

        if (specialty != ReviewSpecialty.All)
        {
            filteredFiles = filteredFiles
                .Where(file => LanguageDetector.DetectSpecialty(file.Path!) == specialty)
                .ToList();
        }

        IReadOnlyList<string> modulesChanged = filteredFiles
            .Select(f => f.Path!.Contains('/') ? f.Path.Split('/')[0] : "(root)")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();


        var (totalAdditions, totalDeletions) = await CompleteGitFileSummaryAsync(baseCommit, targetCommit, changesByPath, filteredFiles, cancellationToken);

        return new ReviewManifest
        {
            GeneratedAt = DateTime.UtcNow,
            ChangeSet = new ChangeSet
            {
                BaseCommit = baseCommit,
                TargetCommit = targetCommit,
                Commits = commits                 
            },
            Summary = new Summary
            {
                TotalAdditions = totalAdditions,
                TotalDeletions = totalDeletions,
                TotalFilesChanged = filteredFiles.Count,
                ModulesChanged = modulesChanged
            },
            Files = filteredFiles           
        };
    }

    private async Task<(int totalAdditions, int totalDeletions)> CompleteGitFileSummaryAsync(string baseCommit, string targetCommit,
        Dictionary<string, GitFile> changesByPath, IReadOnlyList<GitFile> filteredFiles, CancellationToken cancellationToken = default)
    {
        int totalAdditions = 0;
        int totalDeletions = 0;

        foreach (var file in filteredFiles)
        {
            file.Language = LanguageDetector.Detect(file.Path!);
            file.Specialty = LanguageDetector.DetectSpecialty(file.Path!)?.ToString();

            if (changesByPath.TryGetValue(file.Path!, out GitFile? summary) && summary != null)
            {
                file.Additions = summary.Additions;
                file.Deletions = summary.Deletions;
                totalAdditions += summary.Additions ?? 0;
                totalDeletions += summary.Deletions ?? 0;
            }

            // Obtenemos el patch (si es un archivo binario o vacío, simplemente queda string.Empty)
            string patch = await gitService.DiffFilePatchAsync(baseCommit, targetCommit, file.Path!, cancellationToken);
            file.Patch = patch;
        }

        return (totalAdditions, totalDeletions);
    }
}
