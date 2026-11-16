namespace McpFileInspector.ContextGenerator.Services;

public static class GitCommands
{
    // Obtiene  una lista de texto plano con las rutas relativas de los archivos modificados entre ambos commits.
    public static string DiffNameOnly(string baseCommit, string targetCommit) =>
        $"diff --no-ext-diff --no-color --name-only {baseCommit} {targetCommit}";

    // Obtiene solo lista de archivos y su estado (M, A, D, R)
    public static string DiffNameStatus(string baseCommit, string targetCommit) =>
        $"diff --no-ext-diff --no-color --name-status {baseCommit} {targetCommit}";

    // Obtiene métricas numéricas (+/-) por archivo
    public static string DiffNumStat(string baseCommit, string targetCommit) =>
        $"diff --no-ext-diff --no-color --numstat {baseCommit} {targetCommit}";

    // Obtiene el patch de un archivo específico con el padding de contexto deseado (ej. 60 líneas)
    public static string DiffFilePatch(string baseCommit, string targetCommit, string filePath, int contextLines = 60) =>
        $"diff --no-ext-diff --no-color -U{contextLines} {baseCommit} {targetCommit} -- \"{filePath}\"";

    // Obtiene información del commit o PR (autor, fecha, mensaje)
    public static string LogSummary(string commitSha) =>
        $"log -1 --no-color --format=\"%h|%an|%ae|%s\" {commitSha}";

    // Obtiene la lista de SHAs entre base y target excluyendo merges
    public static string RevList(string baseCommit, string targetCommit) =>
        $"rev-list --no-merges {baseCommit}..{targetCommit}";

    // Obtiene el listado de commits con hash, autor y asunto entre base y target
    public static string LogCommits(string baseCommit, string targetCommit) =>
        $"log --no-merges --no-color --format=\"%H|%an|%s\" {baseCommit}..{targetCommit}";
}
