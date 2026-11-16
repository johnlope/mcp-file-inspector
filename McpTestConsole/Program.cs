using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using OpenAI;
using System.ClientModel;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Iniciando cliente de prueba MCP (stdio) ===");

        // Ruta al ensamblado del servidor MCP resuelta dinámicamente
        var serverDllPath = typeof(McpFileInspector.Services.FileService).Assembly.Location;

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

        // 2. Credenciales y Endpoint con API Key (Gateway /openai/v1 de Azure Foundry)
        var endpoint = new Uri("https://<your-endpoint>.openai.azure.com/");
        var apiKey = "<your-api-key>";


        // 3. Crear cliente OpenAI estándar apuntando al Endpoint de Foundry
        var openAiOptions = new OpenAIClientOptions { Endpoint = endpoint };
        OpenAIClient openAiClient = new(new ApiKeyCredential(apiKey), openAiOptions);

        // 4. Crear IChatClient con invocación automática de funciones/tools
        IChatClient chatClient = openAiClient
            .GetChatClient("gpt-4.1-mini") // Nombre de tu deployment en Foundry
            .AsIChatClient()
            .AsBuilder()
            .UseFunctionInvocation() // Ejecuta automáticamente la tool en tu servidor MCP y le devuelve el resultado al modelo
            .Build();

        List<ChatMessage> messages =
        [
            new(ChatRole.System,
                "Eres un revisor de código senior especializado en bases de datos y arquitectura limpia.\n" +
                "Tienes acceso a herramientas MCP locales para explorar el repositorio.\n" +
                "Reglas obligatorias:\n" +
                "1. El nombre del repositorio a consultar siempre es 'GHT-WF'.\n" +
                "2. ANTES de dar cualquier opinión o recomendación, es obligatorio que uses las herramientas para localizar y leer el código fuente completo de los archivos.\n" +
                "3. Basa tus críticas únicamente en el código real leído, no en suposiciones.\n" +
                "4. Evalúa mantenibilidad, duplicidad, rendimiento y viabilidad de unificación."),

            new(ChatRole.User,
                "Localiza en el repositorio los archivos 'GenerateKruegerLabels.sql' y 'GenerateKruegerReport.sql'. " +
                "Lee el contenido de ambos procedimientos y realiza un análisis técnico comparativo: " +
                "¿comparten lógica duplicada?, ¿qué mejoras de rendimiento o buenas prácticas sugieres?, y " +
                "¿es viable y recomendable unificarlos en un único procedimiento almacenado? Justifica con base en el código.")
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