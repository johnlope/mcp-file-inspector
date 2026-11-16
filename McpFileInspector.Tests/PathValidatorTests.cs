using System;
using McpFileInspector.Services;
using McpFileInspector.Tests.Fixtures;
using Xunit;

namespace McpFileInspector.Tests;

public class PathValidatorTests
{
    private readonly IPathValidator _pathValidator;
    
    public PathValidatorTests()
    {
        var fixture = new ConfigurationFixture();
        _pathValidator = new PathValidator(fixture.McpConfiguration);
    }

    [Fact]
    public void Constructor_ShouldInitializeSuccessfully()
    {
        // Act
        Assert.NotNull(_pathValidator);
    }
}
