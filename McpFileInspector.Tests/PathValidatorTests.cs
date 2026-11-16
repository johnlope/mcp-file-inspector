using System;
using System.IO;
using FluentAssertions;
using McpFileInspector.Configurations;
using McpFileInspector.Services;
using McpFileInspector.Tests.Fixtures;
using Xunit;

namespace McpFileInspector.Tests;

public class PathValidatorTests
{
    private readonly IPathValidator _pathValidator;
    private readonly string _tempRepoDir;

    public PathValidatorTests()
    {
        // Creamos una carpeta temporal aislada y real para pruebas unitarias sin depender de rutas fijas del disco
        _tempRepoDir = Path.Combine(Path.GetTempPath(), "McpPathValidatorTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRepoDir);

        var config = new McpConfiguration
        {
            AllowedRoots = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["TestRepo"] = _tempRepoDir
            }
        };

        _pathValidator = new PathValidator(config);
    }

    [Fact]
    public void Constructor_ShouldInitializeSuccessfully()
    {
        Assert.NotNull(_pathValidator);
    }

    [Fact]
    public void GetRepositoryRoot_WithUnknownRepo_ShouldThrowUnauthorizedAccessException()
    {
        var action = () => _pathValidator.GetRepositoryRoot("RepoInexistente");
        action.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("*Repository not allowed*");
    }

    [Fact]
    public void GetRepositoryRoot_WithNullOrEmpty_ShouldThrowArgumentException()
    {
        var action = () => _pathValidator.GetRepositoryRoot("");
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ValidateAndResolvePath_WithValidRoot_ShouldReturnCanonicalFullPath()
    {
        var result = _pathValidator.ValidateAndResolvePath("TestRepo", "");
        result.Should().Be(Path.GetFullPath(_tempRepoDir));
    }

    [Fact]
    public void ValidateAndResolvePath_WithValidExistingFile_ShouldResolveCorrectly()
    {
        var testFile = Path.Combine(_tempRepoDir, "subfolder", "test.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(testFile)!);
        File.WriteAllText(testFile, "// content");

        var result = _pathValidator.ValidateAndResolvePath("TestRepo", "subfolder/test.cs");
        result.Should().Be(Path.GetFullPath(testFile));
    }

    [Fact]
    public void ValidateAndResolvePath_WhenPathDoesNotExist_ShouldThrowFileNotFoundException()
    {
        var action = () => _pathValidator.ValidateAndResolvePath("TestRepo", "archivo_que_no_existe.cs");
        action.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void ValidateAndResolvePath_WithPathTraversalAttempt_ShouldThrowUnauthorizedAccessException()
    {
        // Intento de escapar de la raíz con ../..
        var action = () => _pathValidator.ValidateAndResolvePath("TestRepo", @"..\..\windows\system32\calc.exe");
        action.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("*outside the repository boundaries*");
    }
}

