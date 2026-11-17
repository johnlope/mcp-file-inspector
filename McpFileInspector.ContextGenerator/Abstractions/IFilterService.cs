using System;

namespace McpFileInspector.ContextGenerator.Abstractions;

public interface IFilterService
{
    Task<bool> FilterAsync(string filePath, CancellationToken cancellationToken = default);
}
