using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;

public class FileService
{
    private readonly HashSet<string> _allowedExtensions;
    private readonly HashSet<string> _excludedDirectories;
    private readonly HashSet<string> _excludedExtensions;


    public FileService(IConfiguration configuration)
    {
        // Cargar y normalizar las extensiones permitidas en un HashSet (insensible a mayúsculas)
        var extensions = configuration.GetSection("McpSettings:AllowedExtensions")
            .Get<List<string>>() ?? new List<string>();

        _allowedExtensions = new HashSet<string>(extensions, StringComparer.OrdinalIgnoreCase);

        var excludedextensions = configuration.GetSection("McpSettings:ExcludedExtensions")
            .Get<List<string>>() ?? new List<string>();

        _excludedExtensions = new HashSet<string>(excludedextensions, StringComparer.OrdinalIgnoreCase);

        var folders = configuration.GetSection("McpSettings:ExcludedDirectories")
            .Get<List<string>>() ?? new List<string>();

        _excludedDirectories = new HashSet<string>(folders, StringComparer.OrdinalIgnoreCase);

    }

    public string GetFilteredFiles(string requestedPath)
    {
        // Validar que la clave de la raíz exista en la configuración
        if (!Directory.Exists(requestedPath))
        {
            throw new DirectoryNotFoundException($"The directory '{requestedPath}' does not exist.");
        }

       // Obtener archivos de forma recursiva ignorando carpetas excluidas
        var files = GetFilesRecursive(requestedPath, requestedPath).ToList();

        return string.Join(Environment.NewLine, files);
    }

    private IEnumerable<string> GetFilesRecursive(string currentDir, string rootDir)
    {
        // Si el nombre de la carpeta actual está en la lista de exclusión, la saltamos por completo
        string dirName = Path.GetFileName(currentDir);
        if (_excludedDirectories.Contains(dirName))
        {
            yield break;
        }

        // Obtener archivos de la carpeta actual que cumplan con la extensión permitida
        string[] files;
        try
        {
            files = Directory.GetFiles(currentDir);
        }
        catch
        {
            yield break; // Evita errores si no hay permisos de lectura en alguna subcarpeta
        }

        foreach (var file in files)
        {
            // Validar si termina con alguno de los sufijos prohibidos (ej: .settings.json, .min.js)
            bool matchesExcludedSuffix = _excludedExtensions.Any(extension => 
                file.EndsWith(extension, StringComparison.OrdinalIgnoreCase));

            if (_allowedExtensions.Contains(Path.GetExtension(file)) && !matchesExcludedSuffix)
            {
                yield return Path.GetRelativePath(rootDir, file);
            }
        }

        // Recorrer las subcarpetas de manera recursiva
        string[] subDirs;
        try
        {
            subDirs = Directory.GetDirectories(currentDir);
        }
        catch
        {
            yield break;
        }

        foreach (var subDir in subDirs)
        {
            foreach (var file in GetFilesRecursive(subDir, rootDir))
            {
                yield return file;
            }
        }
    }
}