using McpFileInspector.Tests.Fixtures;

namespace McpFileInspector.Tests;

public class FileServiceTests
{
    private readonly FileService _fileService;

    public FileServiceTests()
    {
        var fixture = new ConfigurationFixture();
        _fileService = new FileService(fixture.Configuration);
    }

    [Fact]
    public void Constructor_ShouldInitializeSuccessfully()
    {
        Assert.NotNull(_fileService);
    }

    [Fact]
    public void GetFilteredFilesTest()
    {
        var files = _fileService.GetFilteredFiles("C:/GHT/Test/");
        Assert.True(files.Count > 0);
    }

    [Fact]
    public void InvalidPathTest()
    {
        try
        {
            var files = _fileService.GetFilteredFiles("D:/GHT/");
        }
        catch (System.IO.DirectoryNotFoundException ex)
        {
            Assert.NotNull(ex);
        }
    }

    [Fact]
    public async Task SearchContentTest()
    {
        var files = await _fileService.SearchContentAsync("C:/GHT/GHT-Reviews", "*.cs", "using Microsoft.AspNetCore.Http", Model.SearchMode.Text);
        Assert.NotNull(files);       
        Assert.True(files.Count > 0);
    }

}
