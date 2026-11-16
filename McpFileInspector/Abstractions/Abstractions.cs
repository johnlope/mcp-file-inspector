using System;

namespace McpFileInspector.Abstractions;

/// <summary>
/// Outcome of a read-only code query. A denied query (path outside the copy, excluded file or limit
/// reached) is a normal result with <see cref="Success"/> false, so the agent can adapt.
/// </summary>
public sealed record CodeReadResult(bool Success, string? Content, string? Error)
{
    public static CodeReadResult Ok(string content) => new(true, content, null);

    public static CodeReadResult Denied(string error) => new(false, null, error);
}

public sealed record CodeQueryRecord(string QueryType, string FilePath, int? StartLine, int? EndLine);
