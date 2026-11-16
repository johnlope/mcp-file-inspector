Install ModelContextProtocol and Create MCP

```powershell
dotnet new install ModelContextProtocol
```


```
dotnet new sln -n McpFileInspector
dotnet new mcpserver -n McpFileInspector
dotnet new xunit -n McpFileInspector.Tests

dotnet sln McpFileInspector.slnx add .\McpFileInspector\McpFileInspector.csproj
dotnet sln McpFileInspector.slnx add .\McpFileInspector.Tests\McpFileInspector.Tests.csproj
```

Install Packages

```powershell
dotnet add McpFileInspector.Tests package Moq
dotnet add McpFileInspector.Tests package FluentAssertions
dotnet add McpFileInspector.Tests package Microsoft.NET.Test.Sdk
```

Run MCP

```powershell
npx @modelcontextprotocol/inspector dotnet run
```

Publicar MCP (dll)

```powershell
dotnet publish McpFileInspector.csproj -c Release -r win-x64 --self-contained false
```

Kill MCP

```powershell
taskkill /F /PID 26616

taskkill /F /IM dotnet.exe
```

