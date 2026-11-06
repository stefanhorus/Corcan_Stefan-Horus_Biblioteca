using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Intalnirea1.Data;
using Intalnirea1.Models;
using Intalnirea1.Services;

namespace Intalnirea1.Controllers;

public class AdminController(LibraryContext db, RecommenderTrainer trainer) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewBag.Ratings = await db.Rating.CountAsync();
        return View();
    }

    // GET: Admin/Dashboard — toate agregările se calculează în SQL
    public async Task<IActionResult> Dashboard()
    {
        var ratings = await db.Rating.CountAsync();
        var readers = await db.Rating.Select(r => r.ReaderId).Distinct().CountAsync();
        var average = ratings > 0 ? await db.Rating.AverageAsync(r => r.Stars) : 0;

        var counts = await db.Rating
            .GroupBy(r => r.Stars)
            .Select(g => new StarCount(g.Key, g.Count()))
            .ToListAsync();
        // toate cele 5 bare, și pentru notele care nu apar deloc
        var distribution = Enumerable.Range(1, 5)
            .Select(s => counts.FirstOrDefault(c => c.Stars == s) ?? new StarCount(s, 0))
            .ToList();

        // [S2] B1: media evaluărilor pe autor
        // Sortarea se face pe agregat (g.Average), nu pe proprietatea record-ului:
        // EF nu poate traduce în SQL un OrderBy pe un obiect construit cu new AuthorAverage(...).
        var byAuthor = (await db.Rating
                .Where(r => r.Book.AuthorID != null)
                .GroupBy(r => new { r.Book.Author!.ID, r.Book.Author.FirstName, r.Book.Author.LastName })
                .OrderByDescending(g => g.Average(r => r.Stars))
                .Select(g => new { g.Key.FirstName, g.Key.LastName,
                    Average = g.Average(r => r.Stars), Count = g.Count() })
                .ToListAsync())
            .Select(a => new AuthorAverage(a.FirstName + " " + a.LastName, a.Average, a.Count))
            .ToList();

        return View(new DashboardViewModel(ratings, readers, average, distribution, byAuthor));
    }

    // GET: Admin/Log?readerId=5&from=2026-11-01&to=2026-11-06&sortOrder=date&pageNumber=2
    public async Task<IActionResult> Log(int? readerId, DateTime? from, DateTime? to,
        string? sortOrder, int? pageNumber)
    {
        ViewData["ReaderId"] = readerId;
        ViewData["From"] = from?.ToString("yyyy-MM-dd");
        ViewData["To"] = to?.ToString("yyyy-MM-dd");
        ViewData["CurrentSort"] = sortOrder;
        ViewData["DateSortParm"] = sortOrder == "date" ? "" : "date";   // implicit: cele mai noi primele

        var logs = db.RecommendationLog.AsNoTracking();
        if (readerId != null)
            logs = logs.Where(l => l.ReaderId == readerId);
        if (from != null)
            logs = logs.Where(l => l.CreatedAt >= from.Value.Date);
        if (to != null)
            logs = logs.Where(l => l.CreatedAt < to.Value.Date.AddDays(1));   // „până la” include toată ziua

        logs = sortOrder == "date"
            ? logs.OrderBy(l => l.CreatedAt).ThenBy(l => l.ID)
            : logs.OrderByDescending(l => l.CreatedAt).ThenByDescending(l => l.ID);

        const int pageSize = 10;
        return View(await PaginatedList<RecommendationLog>.CreateAsync(logs, pageNumber ?? 1, pageSize));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportRatings(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            TempData["Msg"] = "Alegeți un fișier CSV.";
            return RedirectToAction(nameof(Index));
        }

        var ratings = new List<Rating>();
        using var reader = new StreamReader(file.OpenReadStream());
        await reader.ReadLineAsync(); // sărim antetul
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            var p = line.Split(',');
            if (p.Length < 3 || !int.TryParse(p[0], out var u) ||
                !int.TryParse(p[1], out var b) || !int.TryParse(p[2], out var s))
                continue;
            ratings.Add(new Rating { ReaderId = u, BookID = b, Stars = s, CreatedAt = DateTime.UtcNow });
        }

        db.Rating.AddRange(ratings);
        await db.SaveChangesAsync();
        TempData["Msg"] = $"Au fost importate {ratings.Count} evaluări.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Retrain()
    {
        var (rmse, count) = await trainer.TrainAsync();
        TempData["Msg"] = $"Model reantrenat pe {count} evaluări. RMSE = {rmse:0.00}";
        return RedirectToAction(nameof(Index));
    }
}
