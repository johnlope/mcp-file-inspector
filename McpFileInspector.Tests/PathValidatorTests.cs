using System;
using McpFileInspector.Tests.Fixtures;
using Xunit;

namespace McpFileInspector.Tests;

public class PathValidatorTests
{
    private readonly PathValidator _pathValidator;
    
    public PathValidatorTests()
    {
         var fixture = new ConfigurationFixture();
        _pathValidator = new PathValidator(fixture.Configuration);
    }

    [Fact]
    public void Constructor_ShouldInitializeSuccessfully()
    {
        // Act
        Assert.NotNull(_pathValidator);
    }
}
