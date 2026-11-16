using System;
using System.Diagnostics;

namespace McpFileInspector.ContextGenerator.Services;

public class GitProcessRunner
{
    private readonly string workingDirectory;

    public GitProcessRunner(string workingDirectory)
    {
        this.workingDirectory = workingDirectory;
    }

    public async Task<string> ExecuteAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken = default)
    {
        Action<ProcessStartInfo> configureArguments = startInfo =>
        {
            foreach (var argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }
        };

        return await ExecuteAsync(configureArguments, string.Join(' ', arguments), cancellationToken);
    }

    public async Task<string> ExecuteAsync(Action<ProcessStartInfo> configureArguments, string displayArguments, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo("git")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = new System.Text.UTF8Encoding(false),
            StandardErrorEncoding = new System.Text.UTF8Encoding(false),
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        configureArguments(startInfo);

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var error = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"git {displayArguments} failed: {error}");
        }

        return output;
    }

    public async Task<string> ExecuteAsync(string arguments, CancellationToken cancellationToken = default)
    {
        Action<ProcessStartInfo> configureArguments = startInfo =>
        {
            startInfo.Arguments = arguments;
        };

        return await ExecuteAsync(configureArguments, string.Join(' ', arguments), cancellationToken);
    }
}
