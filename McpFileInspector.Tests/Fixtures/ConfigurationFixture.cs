using McpFileInspector.Configurations;
using Microsoft.Extensions.Configuration;

namespace McpFileInspector.Tests.Fixtures;

public class ConfigurationFixture
{
    public IConfiguration Configuration { get; }
    public CodeContextOptions CodeContextOptions { get; }
    public McpConfiguration McpConfiguration { get; }

    public ConfigurationFixture()
    {
        Configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        CodeContextOptions = Configuration
            .GetSection(CodeContextOptions.SectionName)
            .Get<CodeContextOptions>() ?? new CodeContextOptions();

        McpConfiguration = Configuration
            .GetSection(McpConfiguration.SectionName)
            .Get<McpConfiguration>() ?? new McpConfiguration();
    }
}