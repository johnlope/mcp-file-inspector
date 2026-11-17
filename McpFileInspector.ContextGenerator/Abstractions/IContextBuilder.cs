using System;
using McpFileInspector.ContextGenerator.Models;

namespace McpFileInspector.ContextGenerator.Abstractions;

public interface IContextBuilder
{
    Task<ReviewManifest> BuildReviewManifestAsync(string baseCommit, string targetCommit, ReviewSpecialty specialty = ReviewSpecialty.All, CancellationToken cancellationToken = default);
}
