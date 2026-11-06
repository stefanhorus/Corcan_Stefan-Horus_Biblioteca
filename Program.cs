using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Intalnirea1.Data;
using Intalnirea1.Models;
using Microsoft.Extensions.ML;
using Intalnirea1.Services;
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
    async (int id, int? top, LibraryContext db,
           PredictionEnginePool<BookRating, BookRatingPrediction> pool) =>
    {
        var n = Math.Clamp(top ?? 5, 1, 20);

        var rated = await db.Rating.Where(r => r.ReaderId == id)
            .Select(r => r.BookID).ToListAsync();
        var candidates = await db.Book.AsNoTracking()
            .Where(b => !rated.Contains(b.ID))
            .Select(b => new { b.ID, b.Title }).ToListAsync();

        var recs = candidates
            .Select(b => new RecommendationDto(b.ID, b.Title,
                pool.Predict("BookRecommender",
                    new BookRating { UserId = id, BookId = b.ID }).Score, false))
            .Where(r => !float.IsNaN(r.Score))
            .OrderByDescending(r => r.Score)
            .Take(n).ToList();

        if (recs.Count == 0) // cold start: cele mai bine evaluate cărți (minimum P evaluări)
        {
            const int minRatings = 5; // [A3] A0: pragul pentru cold start
            var popular = await db.Rating
                .GroupBy(r => r.BookID)
                .Where(g => g.Count() >= minRatings)
                .Select(g => new { BookID = g.Key, Avg = g.Average(r => r.Stars) })
                .OrderByDescending(x => x.Avg).Take(n)
                .ToListAsync();
            var ids = popular.Select(p => p.BookID).ToList();
            var titles = await db.Book.Where(b => ids.Contains(b.ID))
                .ToDictionaryAsync(b => b.ID, b => b.Title);
            recs = popular.Select(p => new RecommendationDto(
                p.BookID, titles[p.BookID], (float)p.Avg, true)).ToList();
        }

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

app.Run();
