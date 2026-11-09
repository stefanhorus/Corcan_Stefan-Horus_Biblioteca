using System.Diagnostics;
using BibliotecaGrpc;
using Grpc.Core;
using Grpc.Net.Client;

// Portul https al aplicației Biblioteca (Properties/launchSettings.json, profilul „https”)
const string Address = "https://localhost:7245";

using var channel = GrpcChannel.ForAddress(Address);
var client = new Recommender.RecommenderClient(channel);

while (true)
{
    Console.WriteLine();
    Console.WriteLine("1 · Unary   2 · Server streaming   3 · Client streaming   0 · Ieșire");
    Console.Write("Alegeți: ");
    var choice = Console.ReadLine()?.Trim();
    if (choice is null or "0") break;

    try
    {
        switch (choice)
        {
            case "1": await Unary(); break;
            case "2": await ServerStreaming(); break;
            case "3": await ClientStreaming(); break;
            default: Console.WriteLine("Opțiune necunoscută."); break;
        }
    }
    catch (RpcException ex)
    {
        Console.WriteLine($"Eroare gRPC: {ex.StatusCode} — {ex.Status.Detail}");
    }
}

// 1 · un mesaj trimis, un mesaj primit
async Task Unary()
{
    var readerId = Ask("ReaderId", 5);
    var top = Ask("Top", 5);
    var reply = await client.RecommendAsync(new RecommendRequest { ReaderId = readerId, Top = top });
    foreach (var b in reply.Books)
        Console.WriteLine($"  {b.BookId,3}  {b.Title,-45} scor {b.Score:0.00}  fallback={b.Fallback}");
}

// 2 · un mesaj trimis, recomandările vin pe rând
async Task ServerStreaming()
{
    var readerId = Ask("ReaderId", 5);
    var top = Ask("Top", 5);
    var sw = Stopwatch.StartNew();
    using var call = client.StreamRecommendations(new RecommendRequest { ReaderId = readerId, Top = top });
    await foreach (var b in call.ResponseStream.ReadAllAsync())
        Console.WriteLine($"  [{sw.ElapsedMilliseconds,5} ms]  {b.BookId,3}  {b.Title,-45} scor {b.Score:0.00}  fallback={b.Fallback}");
}

// 3 · mai multe evaluări trimise pe rând, un singur rezumat la final
async Task ClientStreaming()
{
    var readerId = Ask("ReaderId", 5);
    var firstBook = Ask("BookId pentru prima evaluare", 10);
    var ratings = new[] { (firstBook, 5), (11, 4), (12, 9) };   // a treia are 9 stele (invalidă)

    using var call = client.RateBooks();
    foreach (var (bookId, stars) in ratings)
    {
        Console.WriteLine($"  trimit: cititor {readerId}, carte {bookId}, {stars} stele");
        await call.RequestStream.WriteAsync(new RatingInput { ReaderId = readerId, BookId = bookId, Stars = stars });
    }
    await call.RequestStream.CompleteAsync();

    var summary = await call;
    Console.WriteLine($"  salvate: {summary.Saved}, respinse: {summary.Rejected}, media: {summary.Average:0.00}");
}

static int Ask(string label, int fallback)
{
    Console.Write($"{label} (Enter = {fallback}): ");
    return int.TryParse(Console.ReadLine(), out var v) ? v : fallback;
}
