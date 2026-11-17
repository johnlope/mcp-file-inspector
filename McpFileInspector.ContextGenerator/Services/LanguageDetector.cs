using System;

namespace McpFileInspector.ContextGenerator.Services;

public static class LanguageDetector
{
    private static readonly Dictionary<string, string> ExtensionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        [".cs"] = "csharp",
        [".sql"] = "sql",
        [".js"] = "javascript",
        [".ts"] = "typescript",
        [".jsx"] = "javascript",
        [".tsx"] = "typescript",
        [".vb"] = "vbnet",
        [".aspx"] = "aspx",
        [".ascx"] = "aspx",
        [".html"] = "html",
        [".htm"] = "html",
        [".css"] = "css",
        [".scss"] = "scss",
        [".json"] = "json",
        [".xml"] = "xml",
        [".yaml"] = "yaml",
        [".yml"] = "yaml",
        [".md"] = "markdown",
        [".sh"] = "bash",
        [".ps1"] = "powershell"
    };

    public static string Detect(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        if (string.IsNullOrEmpty(ext)) return "plaintext";

        return ExtensionMap.TryGetValue(ext, out var lang) ? lang : "plaintext";
    }
}
