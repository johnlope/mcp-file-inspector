using Microsoft.Extensions.Configuration;
using System.IO.Enumeration;
using System.Text.RegularExpressions;
using McpFileInspector.Model;

public class FileService
{
    private readonly HashSet<string> _allowedExtensions;
    private readonly HashSet<string> _excludedDirectories;
    private readonly HashSet<string> _excludedExtensions;
    private readonly HashSet<string> _excludedFiles;
    private readonly int? _maxDegreeOfParallelism;

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

        var files = configuration.GetSection("McpSettings:ExcludedFiles")
            .Get<List<string>>() ?? new List<string>();

        _excludedFiles = new HashSet<string>(files, StringComparer.OrdinalIgnoreCase);

        _maxDegreeOfParallelism = configuration.GetValue<int?>("McpSettings:MaxDegreeOfParallelism");

    }

    public async Task<IReadOnlyList<string>> SearchContentAsync(string repositoryRoot, string searchPattern, string searchTerm,
        SearchMode mode = SearchMode.Text)
    {
        if (string.IsNullOrEmpty(searchTerm))
            return new List<string> { "Search text for search cannot be empty." };

        if (string.IsNullOrWhiteSpace(searchTerm) ||
            searchTerm.Length < 2 ||
            searchTerm.Length > 200)
        {
            throw new ArgumentException("Search text must contain between 2 and 200 characters.");
        }

        var files = SearchFiles(repositoryRoot, searchPattern);

        if (files == null || !files.Any())
            throw new FileNotFoundException("No files found matching the search pattern.");

        if (mode == SearchMode.Text)
        {
            return await SearchTextAsync(repositoryRoot, files, searchTerm, caseSensitive: true);
        }

        var searchRegex = new Regex(searchTerm);
        return await SearchRegexAsync(repositoryRoot,files, searchRegex);
    }

    /// <summary>
    /// Busca texto plano en una lista prefiltrada de archivos de forma asíncrona y paralela.
    /// </summary>
    private async Task<IReadOnlyList<string>> SearchTextAsync(
        string repositoryRoot,
        IEnumerable<string> filePaths,
        string searchTerm,
        bool caseSensitive = false,
        CancellationToken cancellationToken = default)
    {
        //  var results = new ConcurrentBag<SearchResult>();
        List<string> results = new List<string>();
        var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        var options = new ParallelOptions
        {
            CancellationToken = cancellationToken,
            // Si no se especifica, limitamos por defecto a los núcleos lógicos disponibles 
            // o a un número seguro (ej. Environment.ProcessorCount) para no ahogar la CPU/Disco.
            MaxDegreeOfParallelism = _maxDegreeOfParallelism ?? Math.Max(1, Environment.ProcessorCount / 2)
        };

        // Procesar los archivos en paralelo optimizando el uso de CPU
        await Parallel.ForEachAsync(filePaths, options, async (filePath, ct) =>
        {
            try
            {
                int lineNumber = 0;
                filePath = Path.Combine(repositoryRoot, filePath);
                // File.ReadLinesAsync lee en streaming línea por línea sin cargar todo el archivo en memoria
                await foreach (var line in File.ReadLinesAsync(filePath, ct))
                {
                    lineNumber++;
                    if (line.Contains(searchTerm, comparison))
                    {
                        // results.Add(new SearchResult(filePath, lineNumber, line.Trim()));
                        results.Add($"{filePath}:{lineNumber}:{line.Trim()}");
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Ignorar archivos sin permisos de lectura
            }
            catch (IOException)
            {
                // Ignorar archivos bloqueados por otros procesos
            }
        });

        // Ordenar los resultados por ruta de archivo y número de línea para mantener orden
        //return results.OrderBy(r => r.FilePath).ThenBy(r => r.LineNumber).ToList();
        return results;
    }

    /// <summary>
    /// Versión alternativa si necesitas buscar usando Expresiones Regulares (Regex) en lugar de texto plano.
    /// </summary>
    private async Task<IReadOnlyList<string>> SearchRegexAsync(
        string repositoryRoot,
        IEnumerable<string> filePaths,
        Regex regexPattern,
        CancellationToken cancellationToken = default)
    {
        //   var results = new ConcurrentBag<SearchResult>();
        List<string> results = new List<string>();

        var options = new ParallelOptions
        {
            CancellationToken = cancellationToken,
            // Si no se especifica, limitamos por defecto a los núcleos lógicos disponibles 
            // o a un número seguro (ej. Environment.ProcessorCount) para no ahogar la CPU/Disco.
            MaxDegreeOfParallelism = _maxDegreeOfParallelism ?? Math.Max(1, Environment.ProcessorCount / 2)
        };

        await Parallel.ForEachAsync(filePaths, options, async (filePath, ct) =>
        {
            try
            {
                int lineNumber = 0;
                filePath = Path.Combine(repositoryRoot, filePath);
                await foreach (var line in File.ReadLinesAsync(filePath, ct))
                {
                    lineNumber++;
                    if (regexPattern.IsMatch(line))
                    {
                        // results.Add(new SearchResult(filePath, lineNumber, line.Trim()));
                        results.Add($"{filePath}:{lineNumber}:{line.Trim()}");
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Ignorar archivos sin permisos de lectura
            }
            catch (IOException)
            {
                // Ignorar archivos bloqueados por otros procesos
            }
        });

        //return results.OrderBy(r => r.FilePath).ThenBy(r => r.LineNumber).ToList();
        return results;
    }


    public string ReadFile(string fullPath)
    {
        if (!File.Exists(fullPath))
            return "File not found.";

        return File.ReadAllText(fullPath);
    }

    public IReadOnlyList<string> SearchFiles(string repositoryRoot, string searchPattern)
    {
        if (!Directory.Exists(repositoryRoot))
        {
            throw new DirectoryNotFoundException($"The directory '{repositoryRoot}' does not exist.");
        }

        var files = GetFilesRecursive(repositoryRoot, repositoryRoot);

        files = files.Where(file =>
        {
            var fileName = Path.GetFileName(file);

            return FileSystemName.MatchesSimpleExpression(
                searchPattern,
                fileName,
                ignoreCase: true);
        });


        return [.. files];;
    }

    public IReadOnlyList<string> GetFilteredFiles(string requestedPath)
    {
        // Validar que la clave de la raíz exista en la configuración
        if (!Directory.Exists(requestedPath))
        {
            throw new DirectoryNotFoundException($"The directory '{requestedPath}' does not exist.");
        }

        // Obtener archivos de forma recursiva ignorando carpetas excluidas
        var files = GetFilesRecursive(requestedPath, requestedPath);
        return [.. files];
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

            bool isExcludedFile = _excludedFiles.Contains(Path.GetFileName(file));

            if (_allowedExtensions.Contains(Path.GetExtension(file)) && !matchesExcludedSuffix && !isExcludedFile)
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