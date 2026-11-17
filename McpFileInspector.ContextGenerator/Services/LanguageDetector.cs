using System;
using McpFileInspector.ContextGenerator.Models;

namespace McpFileInspector.ContextGenerator.Services;

public static class LanguageDetector
{
    private static readonly Dictionary<string, (string Language, ReviewSpecialty Specialty)> ExtensionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // Backend
        [".cs"] = ("csharp", ReviewSpecialty.Backend),
        [".vb"] = ("vbnet", ReviewSpecialty.Backend),
        [".fs"] = ("fsharp", ReviewSpecialty.Backend),

        // Database
        [".sql"] = ("sql", ReviewSpecialty.Database),

        // UI / Web Forms / Frontend
        [".aspx"] = ("aspx", ReviewSpecialty.UI),
        [".ascx"] = ("aspx", ReviewSpecialty.UI),
        [".master"] = ("aspx", ReviewSpecialty.UI),
        [".razor"] = ("razor", ReviewSpecialty.UI),
        [".html"] = ("html", ReviewSpecialty.UI),
        [".htm"] = ("html", ReviewSpecialty.UI),
        [".js"] = ("javascript", ReviewSpecialty.UI),
        [".ts"] = ("typescript", ReviewSpecialty.UI),
        [".jsx"] = ("javascript", ReviewSpecialty.UI),
        [".tsx"] = ("typescript", ReviewSpecialty.UI),
        [".css"] = ("css", ReviewSpecialty.UI),
        [".scss"] = ("scss", ReviewSpecialty.UI)
    };

    public static string Detect(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        if (string.IsNullOrEmpty(ext)) return "plaintext";

        return ExtensionMap.TryGetValue(ext, out var info) ? info.Language : "plaintext";
    }

    public static ReviewSpecialty? DetectSpecialty(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        if (string.IsNullOrEmpty(ext)) return null;

        return ExtensionMap.TryGetValue(ext, out var info) ? info.Specialty : null;
    }
}
