using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Azure;
using Azure.AI.OpenAI;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Iniciando cliente de prueba MCP (stdio) ===");

        // Ruta a la DLL compilada de tu servidor MCP
        var serverDllPath = @"C:\Personal\mcp-file-inspector\McpFileInspector\bin\Debug\net10.0\win-x64\McpFileInspector.dll";

        // Configurar el transporte Stdio
        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Command = "dotnet",
            Arguments = [serverDllPath],
            InheritEnvironmentVariables = true
        });

        // Inicializar el cliente usando McpClient.CreateAsync
        await using var mcpClient = await McpClient.CreateAsync(transport);

        Console.WriteLine("\n[1] Conexión establecida exitosamente con el servidor MCP.");

        // 1. Descubrir las herramientas expuestas por tu MCP
        var tools = await mcpClient.ListToolsAsync();
        Console.WriteLine($"\n[2] Herramientas detectadas en el servidor ({tools.Count}):");
        foreach (var tool in tools)
        {
            Console.WriteLine($"   - {tool.Name}: {tool.Description}");
        }

        // 2. Probar la herramienta 'ListFiles'
        Console.WriteLine("\n[3] Probando ejecución directa de la herramienta 'ListFiles'...");

        try
        {
            var result = await mcpClient.CallToolAsync("list_files", new Dictionary<string, object?>
            {
                ["repositoryName"] = "GHT-Reviews" // Directorio de prueba actual
            }, cancellationToken: CancellationToken.None);

            foreach (var content in result.Content)
            {
                var text = result.Content
                    .OfType<TextContentBlock>()
                        .FirstOrDefault()?.Text;
                Console.WriteLine($"Resultado:\n{text}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al ejecutar la herramienta: {ex.Message}");
        }

        Console.WriteLine("\n[4] Consola de pruebas finalizada.");

        // 3. Integración con un modelo de IA (Ej: Azure AI Foundry / OpenAI)

        // MCP Tools
        IList<AITool> aiTools = tools
            .Cast<AITool>()
            .ToList();

        // 2. Credenciales y Endpoint con API Key
        var endpoint = new Uri("https://<your-endpoint>.openai.azure.com/");
        var apiKey = "<your-api-key>";
        var credential = new AzureKeyCredential(apiKey);

        // 3. Crear cliente con AzureKeyCredential
        AzureOpenAIClient azureClient = new(endpoint, credential);

        // 4. Crear IChatClient con invocación automática de funciones/tools
        IChatClient chatClient = azureClient
            .GetChatClient("gpt-4.1-mini") // Nombre de tu deployment en Foundry
            .AsIChatClient()
            .AsBuilder()
            .UseFunctionInvocation() // Ejecuta automáticamente la tool en tu servidor MCP y le devuelve el resultado al modelo
            .Build();

        List<ChatMessage> messages =
        [
            new(ChatRole.System, "Eres un asistente técnico con acceso a herramientas locales para inspeccionar código. " +
                         "Los repositorios disponibles para el parámetro 'repositoryName' son: 'GHT-Reviews'. " +
                         "Si el usuario no especifica uno, utiliza 'GHT-Reviews' por defecto."),
            new(ChatRole.User, "Busca archivos de configuración en el proyecto"),
            new(ChatRole.User, "Si encuentras el archivo reviews.json valida que no tenga claves sensibles y muestra su contenido si no las tiene")
        ];

        Console.WriteLine("\n[5] Probando ejecución del modelo con las herramientas MCP");

        // 5. Llamar al modelo pasando las herramientas
        var response = await chatClient.GetResponseAsync(
            messages,
            new ChatOptions
            {
                Tools = aiTools 
            });

        Console.WriteLine($"\n[ASSISTANT]: {response.Text}");
    }
}