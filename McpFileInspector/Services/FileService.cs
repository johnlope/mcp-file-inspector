using System.Collections.Concurrent;
using System.IO.Enumeration;
using System.Text;
using System.Text.RegularExpressions;
using McpFileInspector.Abstractions;
using McpFileInspector.Configurations;
using McpFileInspector.Model;

namespace McpFileInspector.Services;

public class FileService : IFileService
{
    private readonly CodeContextOptions limits;
    private readonly TimeProvider timeProvider;
    private readonly McpConfiguration mcpConfiguartion;
    private readonly long startedAt;
    private readonly List<CodeQueryRecord> queries = [];
    private readonly object gate = new();
    private int calls;
    private long bytes;

    public FileService(CodeContextOptions limits, TimeProvider timeProvider, McpConfiguration mcpConfiguartion)
    {
        this.limits = limits;
        this.timeProvider = timeProvider;
        this.mcpConfiguartion = mcpConfiguartion;

        startedAt = timeProvider.GetTimestamp();
    }

    public IReadOnlyList<string> GetFilteredFiles(string requestedPath)
    {
        // Validar que la clave de la raíz exista en la configuración
        if (!Directory.Exists(requestedPath))
        {
            throw new DirectoryNotFoundException($"The directory '{requestedPath}' does not exist.");
        }

        if (TryBeginCall() is { } limit)
        {
            return [limit.Error ?? limit.Content ?? "Limit reached"];
        }

        int maxFiles = limits.MaxFilesToList > 0 ? limits.MaxFilesToList : 1000;

        // Obtener archivos de forma recursiva ignorando carpetas excluidas y limitando por cantidad
        var acceptedFiles = GetFilesRecursive(requestedPath, requestedPath)
            .Take(maxFiles)
            .ToList();

        Record(new CodeQueryRecord("GetFilteredFiles", requestedPath, null, null));
        return acceptedFiles;
    }

    public async Task<SearchResult> ReadFileAsync(string fullPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"The file '{fullPath}' does not exist.");

        string filePath = Path.GetFileName(fullPath);

        if (TryBeginCall() is { } limit)
        {
            return new SearchResult
            {
                File = filePath,
                Content = limit.Error ?? limit.Content ?? string.Empty
            };
        }

        await using var stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite,
            bufferSize: 4096,
            useAsync: true);

        var fileLength = stream.Length;
        var maxBytesToRead = checked(limits.MaxFileSize + 1);
        var bufferSize = (int)Math.Min(fileLength, maxBytesToRead);
        var buffer = new byte[bufferSize];

        var totalBytesRead = 0;

        while (totalBytesRead < buffer.Length)
        {
            var bytesRead = await stream.ReadAsync(
                buffer.AsMemory(totalBytesRead),
                cancellationToken);

            if (bytesRead == 0)
                break;

            totalBytesRead += bytesRead;
        }

        var truncated = totalBytesRead > limits.MaxFileSize;
        var validBytes = truncated ? limits.MaxFileSize : totalBytesRead;

        if (!TryConsume(validBytes))
        {
            truncated = true;
        }

        var encoding = new UTF8Encoding(false, false);
        var decoder = encoding.GetDecoder();

        var chars = new char[encoding.GetMaxCharCount(validBytes)];

        decoder.Convert(
            buffer, 0, validBytes,
            chars, 0, chars.Length,
            flush: true,
            out _,
            out var charsUsed,
            out _);

        var content = new string(chars, 0, charsUsed);

        string contentResult = truncated
            ? content + "\n[Content truncated]"
            : content;

        Record(new CodeQueryRecord("ReadFile", filePath, null, null));

        return new SearchResult()
        {
            File = fullPath,
            Content = contentResult
        };
    }


    public async Task<SearchResult> ReadFileAsync(string fullPath, int? startLine = null, int? endLine = null, CancellationToken cancellationToken = default)
    {

        if (!File.Exists(fullPath))
            throw new FileNotFoundException("No files found matching the search pattern.");

        string filePath = Path.GetFileName(fullPath);

        if (TryBeginCall() is { } limit)
            return new SearchResult()
            {
                File = filePath,
                Lines = (startLine ?? 1, endLine ?? 1),
                Content = limit.Content ?? string.Empty
            };

        if (startLine < 1 || endLine < 1 || endLine < startLine) throw new ArgumentException("Invalid line range");

        var content = new StringBuilder();
        var lineNumber = 0;
        var truncated = false;
        using (var reader = new StreamReader(fullPath, Encoding.UTF8))
        {
            while (await reader.ReadLineAsync(cancellationToken) is { } line)
            {
                lineNumber++;
                if (startLine is { } first && lineNumber < first) continue;
                if (endLine is { } last && lineNumber > last) break;
                if (!TryConsume(line.Length + 1)) { truncated = true; break; }
                content.Append(line).Append('\n');
            }
        }

        Record(new CodeQueryRecord("ReadFile", filePath, startLine, endLine));
        string contentResult = Finish(content, truncated) ?? string.Empty;

        var result = new SearchResult()
        {
            File = filePath,
            Lines = (startLine ?? 1, endLine ?? lineNumber),
            Content = contentResult
        };

        return result;
    }

    public async Task<FileOutlineResult> GetFileOutlineAsync(string fullPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"The file '{fullPath}' does not exist.");

        string filePath = Path.GetFileName(fullPath);

        if (TryBeginCall() is { } limit)
        {
            return new FileOutlineResult(filePath, 0, []);
        }

        var ext = Path.GetExtension(fullPath).ToLowerInvariant();
        var symbols = new List<FileSymbol>();
        int lineNumber = 0;

        using (var reader = new StreamReader(fullPath, Encoding.UTF8))
        {
            while (await reader.ReadLineAsync(cancellationToken) is { } line)
            {
                lineNumber++;
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;

                FileSymbol? symbol = ext switch
                {
                    ".cs" => ParseCSharpSymbol(trimmed, lineNumber),
                    ".sql" => ParseSqlSymbol(trimmed, lineNumber),
                    ".js" or ".ts" => ParseJsTsSymbol(trimmed, lineNumber),
                    _ => null
                };

                if (symbol != null)
                {
                    symbols.Add(symbol);
                }
            }
        }

        Record(new CodeQueryRecord("GetFileOutline", filePath, 1, lineNumber));
        return new FileOutlineResult(filePath, lineNumber, symbols);
    }

    public SessionSummary GetSessionSummary()
    {
        lock (gate)
        {
            var elapsed = timeProvider.GetElapsedTime(startedAt).TotalSeconds;
            return new SessionSummary(calls, bytes, elapsed, queries.ToList());
        }
    }

    private static FileSymbol? ParseCSharpSymbol(string line, int lineNumber)
    {
        if (line.StartsWith("//") || line.StartsWith("/*") || line.StartsWith("*")) return null;

        var match = Regex.Match(line, @"\b(class|interface|struct|enum|record)\s+([A-Za-z0-9_]+)", RegexOptions.Compiled);
        if (match.Success)
        {
            return new FileSymbol(match.Groups[1].Value, match.Groups[2].Value, lineNumber, line);
        }

        // Métodos o funciones C# (visibilidad + tipo retorno + nombre + paréntesis)
        var methodMatch = Regex.Match(line, @"\b(public|private|protected|internal|static|async|override|virtual)\s+[\w\<\>\[\]\?,\s]+\s+([A-Za-z0-9_]+)\s*\(", RegexOptions.Compiled);
        if (methodMatch.Success && !line.Contains("=") && !line.Contains(" new "))
        {
            return new FileSymbol("method", methodMatch.Groups[2].Value, lineNumber, line);
        }

        return null;
    }

    private static FileSymbol? ParseSqlSymbol(string line, int lineNumber)
    {
        if (line.StartsWith("--") || line.StartsWith("/*")) return null;

        var match = Regex.Match(line, @"\b(CREATE|ALTER)\s+(PROCEDURE|PROC|FUNCTION|VIEW|TABLE)\s+([\[\]\w\.]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        if (match.Success)
        {
            return new FileSymbol(match.Groups[2].Value.ToUpperInvariant(), match.Groups[3].Value.Replace("[", "").Replace("]", ""), lineNumber, line);
        }

        return null;
    }

    private static FileSymbol? ParseJsTsSymbol(string line, int lineNumber)
    {
        if (line.StartsWith("//") || line.StartsWith("/*") || line.StartsWith("*")) return null;

        var classMatch = Regex.Match(line, @"\bclass\s+([A-Za-z0-9_]+)", RegexOptions.Compiled);
        if (classMatch.Success)
        {
            return new FileSymbol("class", classMatch.Groups[1].Value, lineNumber, line);
        }

        var funcMatch = Regex.Match(line, @"\bfunction\s+([A-Za-z0-9_]+)\s*\(", RegexOptions.Compiled);
        if (funcMatch.Success)
        {
            return new FileSymbol("function", funcMatch.Groups[1].Value, lineNumber, line);
        }

        var arrowOrMethod = Regex.Match(line, @"\b(const|let|var)\s+([A-Za-z0-9_]+)\s*=\s*(?:async\s*)?\([^)]*\)\s*=>", RegexOptions.Compiled);
        if (arrowOrMethod.Success)
        {
            return new FileSymbol("function", arrowOrMethod.Groups[2].Value, lineNumber, line);
        }

        return null;
    }

    public IReadOnlyList<string> SearchFiles(string repositoryRoot, string searchPattern)
    {
        if (!Directory.Exists(repositoryRoot))
        {
            throw new DirectoryNotFoundException($"The directory '{repositoryRoot}' does not exist.");
        }

        if (TryBeginCall() is { } limit)
        {
            return [limit.Error ?? limit.Content ?? "Limit reached"];
        }

        int maxFiles = limits.MaxFilesToList > 0 ? limits.MaxFilesToList : 1000;

        var files = GetFilesRecursive(repositoryRoot, repositoryRoot)
            .Where(file =>
            {
                var fileName = Path.GetFileName(file);
                return FileSystemName.MatchesSimpleExpression(
                    searchPattern,
                    fileName,
                    ignoreCase: true);
            })
            .Take(maxFiles)
            .ToList();

        Record(new CodeQueryRecord("SearchFiles", searchPattern, null, null));
        return files;
    }

    public async Task<IReadOnlyList<SearchResult>> SearchContentAsync(string repositoryRoot, string searchPattern, string searchTerm,
        SearchMode mode = SearchMode.Text)
    {

        if (string.IsNullOrWhiteSpace(searchTerm) ||
            searchTerm.Length < 2 ||
            searchTerm.Length > 200)
        {
            throw new ArgumentException("Search text must contain between 2 and 200 characters.");
        }

        if (TryBeginCall() is { } limit)
        {
            return [new SearchResult
            {
                File = string.Empty,
                Content = limit.Error ?? limit.Content ?? string.Empty
            }];
        }

        var files = SearchFilesInternal(repositoryRoot, searchPattern);

        if (files == null || !files.Any())
            throw new FileNotFoundException("No files found matching the search pattern.");

        IReadOnlyList<SearchResult> results;
        if (mode == SearchMode.Text)
        {
            results = await SearchTextAsync(repositoryRoot, files, searchTerm, caseSensitive: true);
        }
        else
        {
            var searchRegex = new Regex(searchTerm);
            results = await SearchRegexAsync(repositoryRoot, files, searchRegex);
        }

        Record(new CodeQueryRecord("SearchContent", searchPattern, null, null));
        return results;
    }


    private IReadOnlyList<string> SearchFilesInternal(string repositoryRoot, string searchPattern)
    {
        var files = GetFilesRecursive(repositoryRoot, repositoryRoot);

        files = files.Where(file =>
        {
            var fileName = Path.GetFileName(file);

            return FileSystemName.MatchesSimpleExpression(
                searchPattern,
                fileName,
                ignoreCase: true);
        });

        return [.. files];
    }

    private CodeReadResult? TryBeginCall()
    {
        lock (gate)
        {
            if (calls >= limits.MaxCalls) return CodeReadResult.Denied($"Exceeded the maximum number of calls: {limits.MaxCalls}");
            calls++;
        }

        if (TimeExceeded()) return CodeReadResult.Denied($"Awaited time exceeded the maximum of {limits.MaxSeconds} seconds.");

        lock (gate)
        {
            if (bytes >= limits.MaxBytes) return CodeReadResult.Denied($"Exceeded the maximum number of bytes: {limits.MaxBytes}");
        }

        return null;
    }

    private bool HasExceededBytes()
    {
        lock (gate) return bytes >= limits.MaxBytes;
    }

    private bool TryConsume(int count)
    {
        lock (gate)
        {
            if (bytes + count > limits.MaxBytes) { bytes = limits.MaxBytes; return false; }
            bytes += count;
            return true;
        }
    }

    private static string Finish(StringBuilder content, bool truncated) =>
           truncated ? content.Append("[Content truncated]").ToString() : content.ToString();


    private void Record(CodeQueryRecord query)
    {
        lock (gate) queries.Add(query);
    }

    private bool TimeExceeded() => timeProvider.GetElapsedTime(startedAt).TotalSeconds >= limits.MaxSeconds;

    private IEnumerable<string> GetFilesRecursive(string currentDir, string rootDir)
    {
        // Si el nombre de la carpeta actual está en la lista de exclusión, la saltamos por completo
        string dirName = Path.GetFileName(currentDir);
        if (mcpConfiguartion.ExcludedDirectories.Contains(dirName))
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
            bool matchesExcludedSuffix = mcpConfiguartion.ExcludedExtensions.Any(extension =>
                file.EndsWith(extension, StringComparison.OrdinalIgnoreCase));

            bool isExcludedFile = mcpConfiguartion.ExcludedFiles.Contains(Path.GetFileName(file));

            if (mcpConfiguartion.AllowedExtensions.Contains(Path.GetExtension(file)) && !matchesExcludedSuffix && !isExcludedFile)
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

    /// <summary>
    /// Busca texto plano en una lista prefiltrada de archivos de forma asíncrona y paralela.
    /// </summary>
    private async Task<IReadOnlyList<SearchResult>> SearchTextAsync(string repositoryRoot, IEnumerable<string> filePaths,
        string searchTerm, bool caseSensitive = false,
        CancellationToken cancellationToken = default)
    {
        var results = new ConcurrentBag<SearchResult>();
        var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        var options = new ParallelOptions
        {
            CancellationToken = cancellationToken,
            // Si no se especifica, limitamos por defecto a los núcleos lógicos disponibles 
            // o a un número seguro (ej. Environment.ProcessorCount) para no ahogar la CPU/Disco.
            MaxDegreeOfParallelism = limits.MaxDegreeOfParallelism ?? Math.Max(1, Environment.ProcessorCount / 2)
        };

        // Procesar los archivos en paralelo optimizando el uso de CPU
        await Parallel.ForEachAsync(filePaths, options, async (filePath, ct) =>
        {
            if (TimeExceeded() || HasExceededBytes()) return;

            try
            {
                int lineNumber = 0;
                string fullPath = Path.Combine(repositoryRoot, filePath);
                // File.ReadLinesAsync lee en streaming línea por línea sin cargar todo el archivo en memoria
                await foreach (var line in File.ReadLinesAsync(fullPath, ct))
                {
                    if (TimeExceeded() || HasExceededBytes()) break;

                    lineNumber++;
                    if (line.Contains(searchTerm, comparison))
                    {
                        var trimmed = line.Trim();
                        var lineBytes = Encoding.UTF8.GetByteCount(trimmed);

                        if (TryConsume(lineBytes))
                        {
                            results.Add(new()
                            {
                                File = filePath,
                                Line = lineNumber,
                                Content = trimmed
                            });
                        }
                        else
                        {
                            break;
                        }
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
        return results.OrderBy(r => r.File).ThenBy(r => r.Line).ToList();
    }

    /// <summary>
    /// Versión alternativa si necesitas buscar usando Expresiones Regulares (Regex) en lugar de texto plano.
    /// </summary>
    private async Task<IReadOnlyList<SearchResult>> SearchRegexAsync(string repositoryRoot, IEnumerable<string> filePaths, Regex regexPattern,
        CancellationToken cancellationToken = default)
    {
        var results = new ConcurrentBag<SearchResult>();

        var options = new ParallelOptions
        {
            CancellationToken = cancellationToken,
            // Si no se especifica, limitamos por defecto a los núcleos lógicos disponibles 
            // o a un número seguro (ej. Environment.ProcessorCount) para no ahogar la CPU/Disco.
            MaxDegreeOfParallelism = limits.MaxDegreeOfParallelism ?? Math.Max(1, Environment.ProcessorCount / 2)
        };

        await Parallel.ForEachAsync(filePaths, options, async (filePath, ct) =>
        {
            if (TimeExceeded() || HasExceededBytes()) return;

            try
            {
                int lineNumber = 0;
                string fullPath = Path.Combine(repositoryRoot, filePath);
                await foreach (var line in File.ReadLinesAsync(fullPath, ct))
                {
                    if (TimeExceeded() || HasExceededBytes()) break;

                    lineNumber++;
                    if (regexPattern.IsMatch(line))
                    {
                        var trimmed = line.Trim();
                        var lineBytes = Encoding.UTF8.GetByteCount(trimmed);

                        if (TryConsume(lineBytes))
                        {
                            results.Add(new()
                            {
                                File = filePath,
                                Line = lineNumber,
                                Content = trimmed
                            });
                        }
                        else
                        {
                            break;
                        }
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

        return results.OrderBy(r => r.File).ThenBy(r => r.Line).ToList();
    }
}