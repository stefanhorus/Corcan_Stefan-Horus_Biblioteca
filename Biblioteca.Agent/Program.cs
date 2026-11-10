using System.ClientModel;
using System.Text.Json;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using OpenAI;

// Serverul MCP al Bibliotecii (portul https din Properties/launchSettings.json) și modelul local din Ollama
const string McpEndpoint = "https://localhost:7245/mcp";
const string OllamaEndpoint = "http://localhost:11434/v1";
var modelName = args.Length > 0 ? args[0] : "llama3.2";

await using var mcp = await McpClient.CreateAsync(new HttpClientTransport(new HttpClientTransportOptions
{
    Endpoint = new Uri(McpEndpoint),
    Name = "Biblioteca"
}));

Console.WriteLine($"Conectat la {McpEndpoint}");
Console.Write("1 · Inspector   2 · Agent   Alegeți: ");
var choice = Console.ReadLine()?.Trim();

if (choice == "1") await Inspector();
else if (choice == "2") await Agent();

// 1 · fără model de limbaj: descoperim tool-urile și apelăm search_books direct
async Task Inspector()
{
    var tools = await mcp.ListToolsAsync();
    Console.WriteLine($"\nServerul expune {tools.Count} tool-uri:");
    foreach (var tool in tools)
    {
        Console.WriteLine($"\n● {tool.Name}");
        Console.WriteLine($"  descriere: {tool.Description}");
        Console.WriteLine($"  schema:    {tool.JsonSchema.GetRawText()}");
    }

    Console.Write("\nCe caut cu search_books? (Enter = Rebreanu): ");
    var query = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(query)) query = "Rebreanu";

    var result = await mcp.CallToolAsync("search_books", new Dictionary<string, object?> { ["query"] = query });
    Console.WriteLine($"\nRezultat (IsError = {result.IsError ?? false}):");
    foreach (var block in result.Content)
        Console.WriteLine(block is TextContentBlock text ? $"  [text] {text.Text}" : $"  [{block.Type}]");
}

// 2 · modelul primește lista de tool-uri și decide singur pe care le apelează
async Task Agent()
{
    var tools = await mcp.ListToolsAsync();

    IChatClient chat = new OpenAIClient(new ApiKeyCredential("ollama"),
            new OpenAIClientOptions { Endpoint = new Uri(OllamaEndpoint) })
        .GetChatClient(modelName)
        .AsIChatClient()
        .AsBuilder()
        .UseFunctionInvocation(configure: f => f.FunctionInvoker = async (ctx, ct) =>
        {
            // urma agentului: fiecare tool apelat, cu argumentele lui
            Console.WriteLine($"  [tool] {ctx.Function.Name}({JsonSerializer.Serialize(ctx.Arguments)})");
            return await ctx.Function.InvokeAsync(ctx.Arguments, ct);
        })
        .Build();

    var options = new ChatOptions { Tools = [.. tools] };
    Console.WriteLine($"\nAgent cu modelul {modelName}. O întrebare pe linie, linie goală = ieșire.");

    while (true)
    {
        Console.Write("\n> ");
        var question = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(question)) break;

        // fiecare întrebare pornește o conversație nouă
        List<ChatMessage> messages =
        [
            new(ChatRole.System,
                "Ești asistentul Bibliotecii. Pentru informații despre cărți, autori, prețuri și recomandări " +
                "folosește tool-urile disponibile. Răspunde în limba română."),
            new(ChatRole.User, question)
        ];

        var response = await chat.GetResponseAsync(messages, options);
        Console.WriteLine($"\n{response.Text}");
    }
}
