using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.ML;
using Intalnirea1.Data;
using Intalnirea1.Models;

namespace Intalnirea1.Controllers;

public class RecommendationsController(LibraryContext db,
    PredictionEnginePool<BookRating, BookRatingPrediction> pool) : Controller
{
    public async Task<IActionResult> Index(int userId = 1)
    {
        var books = await db.Book.AsNoTracking()
            .Select(b => new { b.ID, b.Title }).ToListAsync();

        var top = books
            .Select(b => new Recommendation(b.Title,
                pool.Predict("BookRecommender",
                    new BookRating { UserId = userId, BookId = b.ID }).Score))
            .Where(r => !float.IsNaN(r.Score))
            .OrderByDescending(r => r.Score)
            .Take(5).ToList();

        ViewBag.UserId = userId;
        return View(top);
    }
}
