using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

var settingsPath = Path.Combine(
    AppContext.BaseDirectory,
    "appsettings.json");

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile(settingsPath, optional: false)
    .Build();

// Add the MCP services: the transport to use (stdio) and the tools to register.
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<RepositoryTools>();

builder.Services.AddSingleton<PathValidator>();
builder.Services.AddSingleton<FileService>();
builder.Services.AddSingleton<IConfiguration>(configuration);

await builder.Build().RunAsync();
