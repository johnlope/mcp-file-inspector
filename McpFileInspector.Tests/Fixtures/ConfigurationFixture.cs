using System;
using Microsoft.Extensions.Configuration;

namespace McpFileInspector.Tests.Fixtures;

public class ConfigurationFixture
{
    public IConfiguration Configuration { get; }

    public ConfigurationFixture()
    {
        Configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();
    }
}