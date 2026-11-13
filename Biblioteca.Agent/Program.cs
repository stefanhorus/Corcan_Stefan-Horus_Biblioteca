// Biblioteca.Agent (actualizat pentru I8)
// 1 = inspector (fara model de limbaj): ce vede clientul MCP
// 2 = agentul: Ollama + tool-urile MCP ale Bibliotecii
// 3 = NOU:cautare în documente (search_documents), fara model: fragmentele si scorurile
using System.Text.Json;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using OpenAI;
using System.ClientModel;

// Portul https al aplicatiei Biblioteca (launchSettings.json)
const string McpEndpoint = "https://localhost:7245/mcp";
const string OllamaEndpoint = "http://localhost:11434/v1";
const string Model = "llama3.2";

var transport = new HttpClientTransport(new HttpClientTransportOptions
{
    Endpoint = new Uri(McpEndpoint)
});
await using McpClient mcp = await McpClient.CreateAsync(transport);
IList<McpClientTool> tools = await mcp.ListToolsAsync();

Console.Write("1 Inspector; 2 Agent; 3 Cautare in documente; 4 Evaluare > ");
switch (Console.ReadLine())
{
    case "1": await Inspector(); break;
    case "3": await CautareDocumente(); break;
    case "4": await Evaluare(); break;
    default: await Agent(); break;
}

// I8 
async Task CautareDocumente()
{
    while (true)
    {
        Console.Write("\nÎntrebare (Enter = ieșire): ");
        var query = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(query)) break;
        Console.Write("Sursa (regulament / carte / Enter = ambele): ");
        var source = Console.ReadLine();
        Console.Write("Câte fragmente (1–8, Enter = 3): ");
        int top = int.TryParse(Console.ReadLine(), out var k) ? k : 3;

        CallToolResult result = await mcp.CallToolAsync("search_documents",
            new Dictionary<string, object?> { ["query"] = query, ["source"] = source, ["top"] = top });

        if (result.IsError == true)
        {
            foreach (var b in result.Content.OfType<TextContentBlock>()) Console.WriteLine($"Eroare: {b.Text}");
            continue;
        }
        foreach (var block in result.Content.OfType<TextContentBlock>())
        {
            using var doc = JsonDocument.Parse(block.Text);
            int i = 1;
            foreach (var hit in doc.RootElement.EnumerateArray())
            {
                Console.WriteLine($"\n{i++}. [{Camp(hit, "score").GetDouble():F3}] " +
                                  $"{Camp(hit, "source")} · {Camp(hit, "title")}");
                Console.WriteLine($"   {Camp(hit, "text")}");
            }
        }
    }
}

// Î10-S1: recall@k pe setul din eval.json, prin același apel MCP ca la opțiunea 3
async Task Evaluare()
{
    const int K = 5;
    var items = JsonSerializer.Deserialize<List<EvalItem>>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "eval.json")),
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

    int[] hits = new int[K + 1];            // hits[k] = câte întrebări au fragmentul așteptat între primele k
    Console.WriteLine($"\n{"Întrebare",-62} {"Loc",-8} Scor#1");
    foreach (var item in items)
    {
        CallToolResult result = await mcp.CallToolAsync("search_documents",
            new Dictionary<string, object?> { ["query"] = item.Question, ["source"] = item.Source, ["top"] = K });

        var found = new List<(string Title, double Score)>();
        foreach (var block in result.Content.OfType<TextContentBlock>())
        {
            using var doc = JsonDocument.Parse(block.Text);
            foreach (var hit in doc.RootElement.EnumerateArray())
                found.Add((Camp(hit, "title").GetString() ?? "", Camp(hit, "score").GetDouble()));
        }

        // titlurile din Markdown au "6\. ..." (punctul escapat), așa că scoatem "\" înainte de comparație
        int rank = found.FindIndex(f => f.Title.Replace("\\", "").StartsWith(item.Expected,
            StringComparison.OrdinalIgnoreCase)) + 1;
        for (int k = 1; k <= K; k++) if (rank >= 1 && rank <= k) hits[k]++;

        var q = item.Question.Length > 60 ? item.Question[..57] + "..." : item.Question;
        var first = found.Count > 0 ? found[0].Score.ToString("F3") : "-";
        Console.WriteLine($"{q,-62} {(rank > 0 ? rank.ToString() : "negăsit"),-8} {first}");
    }

    Console.WriteLine($"\n{items.Count} întrebări · recall@1 = {100.0 * hits[1] / items.Count:F1}% · " +
                      $"recall@3 = {100.0 * hits[3] / items.Count:F1}% · recall@5 = {100.0 * hits[5] / items.Count:F1}%");
}

// campul unui obiect JSON, indiferent de litere mari/mici (Score sau score)
static JsonElement Camp(JsonElement e, string name) =>
    e.EnumerateObject().First(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)).Value;

// M1
async Task Inspector()
{
    foreach (var t in tools)
    {
        Console.WriteLine($"\n== {t.Name}\n{t.Description}");
        Console.WriteLine(JsonSerializer.Serialize(t.JsonSchema,
            new JsonSerializerOptions { WriteIndented = true }));
    }

    Console.Write("\nCăutați în catalog (search_books): ");
    var query = Console.ReadLine() ?? "";
    CallToolResult result = await mcp.CallToolAsync("search_books",
        new Dictionary<string, object?> { ["query"] = query });

    Console.WriteLine($"IsError: {result.IsError}");
    foreach (var block in result.Content.OfType<TextContentBlock>())
        Console.WriteLine(block.Text);

    // Î9-S2: apelul oricărui alt tool, cu argumentele date ca JSON (de ex. books_by_year)
    while (true)
    {
        Console.Write("\nAlt tool de apelat (Enter = ieșire): ");
        var name = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(name)) break;
        Console.Write("Argumente JSON, de ex. {\"fromYear\":1920,\"toYear\":1939}: ");
        var json = Console.ReadLine();
        var args = string.IsNullOrWhiteSpace(json)
            ? new Dictionary<string, object?>()
            : JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!
                .ToDictionary(a => a.Key, a => (object?)a.Value);

        CallToolResult r = await mcp.CallToolAsync(name.Trim(), args);
        Console.WriteLine($"IsError: {r.IsError}");
        foreach (var block in r.Content.OfType<TextContentBlock>())
            Console.WriteLine(block.Text);
    }
}

// M2
async Task Agent()
{
    IChatClient chat = new ChatClientBuilder(
            new OpenAIClient(new ApiKeyCredential("ollama"),
                             new OpenAIClientOptions { Endpoint = new Uri(OllamaEndpoint) })
                .GetChatClient(Model)
                .AsIChatClient())
        .UseFunctionInvocation()        // execută automat tool-urile cerute de model
        .Build();

    List<ChatMessage> messages =
    [
        new(ChatRole.System,
            "Ești asistentul Bibliotecii FSEGA. Răspunzi în limba română. " +
            "Pentru orice informație despre cărți, cititori sau recomandări folosește tool-urile. " +
            "Nu inventa titluri, prețuri sau autori: dacă tool-urile nu întorc informația, spune că nu o ai. " +
            // I8: regulile vin din regulament, prin search_documents
            "Pentru reguli ale bibliotecii (împrumut, termene, penalități, program) folosește search_documents " +
            "și răspunde doar din fragmentele găsite, menționând secțiunea din regulament. " +
            "Dacă fragmentele nu conțin răspunsul, spune că regulamentul nu precizează.")
    ];
    var options = new ChatOptions { Tools = [.. tools] };

    Console.WriteLine($"Tool-uri disponibile: {string.Join(", ", tools.Select(t => t.Name))}");
    while (true)
    {
        Console.Write("\nTu: ");
        var question = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(question)) break;

        messages.Add(new(ChatRole.User, question));
        ChatResponse response = await chat.GetResponseAsync(messages, options);
        messages.AddMessages(response);

        // urma agentului: ce tool-uri a apelat si cu ce argumente
        foreach (var call in response.Messages.SelectMany(m => m.Contents).OfType<FunctionCallContent>())
        {
            var args = string.Join(", ", (call.Arguments ?? new Dictionary<string, object?>())
                .Select(a => $"{a.Key}={a.Value}"));
            Console.WriteLine($"  [tool] {call.Name}({args})");
        }
        Console.WriteLine($"Asistent: {response.Text}");
    }
}

// o întrebare din eval.json
record EvalItem(string Question, string Source, string Expected);
