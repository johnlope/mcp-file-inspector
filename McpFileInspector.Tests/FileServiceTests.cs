using McpFileInspector.Services;
using McpFileInspector.Tests.Fixtures;

namespace McpFileInspector.Tests;

public class FileServiceTests
{
    private readonly FileService _fileService;

    public FileServiceTests()
    {
        var fixture = new ConfigurationFixture();
        _fileService = new FileService(fixture.CodeContextOptions, TimeProvider.System, fixture.McpConfiguration);
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

    [Fact]
    public async Task GetFileOutlineAsync_ShouldExtractCSharpSymbolsAndProvideSummary()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"OutlineTest_{Guid.NewGuid():N}.cs");
        var code = """
            namespace DemoApp;

            public interface IOrderService
            {
                void Process();
            }

            public class OrderService : IOrderService
            {
                public async Task<int> CalculateTotalAsync(int orderId)
                {
                    return 10;
                }
            }
            """;
        await File.WriteAllTextAsync(tempFile, code);

        try
        {
            var outline = await _fileService.GetFileOutlineAsync(tempFile);

            Assert.NotNull(outline);
            Assert.Contains(outline.Symbols, s => s.Kind == "interface" && s.Name == "IOrderService");
            Assert.Contains(outline.Symbols, s => s.Kind == "class" && s.Name == "OrderService");
            Assert.Contains(outline.Symbols, s => s.Kind == "method" && s.Name == "CalculateTotalAsync");

            var summary = _fileService.GetSessionSummary();
            Assert.NotNull(summary);
            Assert.True(summary.TotalCalls > 0);
            Assert.Contains(summary.Queries, q => q.QueryType == "GetFileOutline");
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task GetFileOutlineAsync_ShouldExtractSqlSymbols()
    {
        var tempSql = Path.Combine(Path.GetTempPath(), $"OutlineTest_{Guid.NewGuid():N}.sql");
        var sql = """
            CREATE PROCEDURE [dbo].[SaveInvoice]
                @InvoiceId INT
            AS
            BEGIN
                SELECT 1;
            END;

            CREATE VIEW [dbo].[vw_ActiveInvoices] AS
            SELECT * FROM Invoices;
            """;
        await File.WriteAllTextAsync(tempSql, sql);

        try
        {
            var outline = await _fileService.GetFileOutlineAsync(tempSql);

            Assert.NotNull(outline);
            Assert.Contains(outline.Symbols, s => s.Kind == "PROCEDURE" && s.Name == "dbo.SaveInvoice");
            Assert.Contains(outline.Symbols, s => s.Kind == "VIEW" && s.Name == "dbo.vw_ActiveInvoices");
        }
        finally
        {
            if (File.Exists(tempSql)) File.Delete(tempSql);
        }
    }
}
