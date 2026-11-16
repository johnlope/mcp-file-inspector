using McpFileInspector.Configurations;
using McpFileInspector.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

// Add the MCP services: the transport to use (stdio) and the tools to register.
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<RepositoryTools>();

// Configuración cargada fuertemente tipada desde builder.Configuration
var codeContextOptions = builder.Configuration
    .GetSection(CodeContextOptions.SectionName)
    .Get<CodeContextOptions>() ?? new CodeContextOptions();

var mcpConfig = builder.Configuration
    .GetSection(McpConfiguration.SectionName)
    .Get<McpConfiguration>() ?? new McpConfiguration();

builder.Services.AddSingleton(codeContextOptions);
builder.Services.AddSingleton(mcpConfig);
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddSingleton<PathValidator>();
builder.Services.AddSingleton<IFileService, FileService>();

await builder.Build().RunAsync();
