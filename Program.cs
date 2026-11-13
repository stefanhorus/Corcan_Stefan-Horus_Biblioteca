using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Intalnirea1.Data;
using Intalnirea1.Models;
using Microsoft.Extensions.ML;
using Intalnirea1.Services;
using Intalnirea1.Rag;
using Microsoft.Extensions.AI;
using OllamaSharp;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<LibraryContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("LibraryContext") ?? throw new InvalidOperationException("Connection string 'LibraryContext' not found.")));

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services
    .AddPredictionEnginePool<BookRating, BookRatingPrediction>()
    .FromFile(modelName: "BookRecommender",
              filePath: "MLModels/BookRecommender.zip",
              watchForChanges: true);

builder.Services.AddPredictionEnginePool<BookText, GenrePrediction>()
    .FromFile(modelName: "GenreClassifier",
              filePath: "MLModels/GenreClassifier.zip", watchForChanges: true);

builder.Services.AddScoped<RecommenderTrainer>();
builder.Services.AddGrpc();
builder.Services.AddScoped<RecommendationService>();

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

// Î8: embeddings prin Ollama și indexul vectorial (regulament + descrierile cărților)
builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(
    new OllamaApiClient(new Uri("http://localhost:11434"), "embeddinggemma"));
builder.Services.AddSingleton<DocumentIndex>();

// Î10-A2: clientul de chat pentru pagina „Întreabă” (RAG clasic)
builder.Services.AddSingleton<IChatClient>(
    new OllamaApiClient(new Uri("http://localhost:11434"), "llama3.2"));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LibraryContext>();
    db.Database.Migrate();          // aplică migrările (înlocuiește Update-Database)
    LibrarySeed.Seed(db);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Prețurile folosesc punctul zecimal (45.50), indiferent de setările regionale ale sistemului.
app.UseRequestLocalization("en-US");

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "start",
    pattern: "start",
    defaults: new { controller = "Home", action = "Index" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapGet("/api/v2/readers/{id:int}/recommendations",
    async (int id, int? top, LibraryContext db, RecommendationService service) =>
    {
        // Aceeași logică (și același prag de cold start) ca serviciul gRPC
        var recs = (await service.RecommendAsync(id, top ?? 5))
            .Select(r => new RecommendationDto(r.BookId, r.Title, r.Score, r.Fallback))
            .ToList();

        // [S1] jurnalul recomandărilor: fiecare apel se salvează
        db.RecommendationLog.Add(new RecommendationLog
        {
            ReaderId = id,
            CreatedAt = DateTime.UtcNow,
            BookIds = string.Join(",", recs.Select(r => r.BookId)),
            Fallback = recs.Any(r => r.Fallback)
        });
        await db.SaveChangesAsync();

        return TypedResults.Ok(recs);
    })
    .WithTags("Recomandări");

app.MapGrpcService<RecommenderGrpcService>();
app.MapGrpcService<ReaderRatingsGrpcService>();
app.MapMcp("/mcp");

// Î8: construim indexul o singură dată, la pornire (câteva secunde)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LibraryContext>();
    var index = app.Services.GetRequiredService<DocumentIndex>();
    await index.BuildAsync(db, Path.Combine(app.Environment.ContentRootPath, "Docs", "regulament.md"));
}

app.Run();
