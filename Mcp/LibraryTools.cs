// Mcp/LibraryTools.cs
// Tool-urile MCP ale Bibliotecii. Numele, descrierile și parametrii de aici sunt tot ce vede clientul
// (inclusiv modelul de limbaj): codul și interogările LINQ rămân pe server.
using System.ComponentModel;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using Intalnirea1.Data;
using Intalnirea1.Services;

namespace Intalnirea1.Mcp;

[McpServerToolType]
public static class LibraryTools
{
    // diacriticele rămân lizibile în JSON (altfel ar apărea ca ă)
    private static readonly JsonSerializerOptions Json = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    [McpServerTool(Name = "search_books"),
     Description("Caută cărți în catalogul Bibliotecii după un cuvânt din titlu sau din numele autorului. " +
                 "Întoarce pentru fiecare carte: ID, titlu, autor, gen, an și preț în lei.")]
    public static async Task<string> SearchBooks(
        LibraryContext db,
        [Description("Textul căutat în titlu sau în numele autorului, de exemplu „Rebreanu” sau „Ion”.")] string query)
    {
        var pattern = $"%{query}%";
        var books = await db.Book.AsNoTracking()
            .Where(b => EF.Functions.Like(b.Title, pattern)
                     || (b.Author != null && (EF.Functions.Like(b.Author.FirstName, pattern)
                                          || EF.Functions.Like(b.Author.LastName, pattern))))
            .OrderBy(b => b.Title)
            .Select(b => new
            {
                b.ID,
                b.Title,
                Author = b.Author != null ? b.Author.FirstName + " " + b.Author.LastName : "",
                Genre = b.Genre != null ? b.Genre.Name : "",
                b.Year,
                b.Price
            })
            .ToListAsync();
        return JsonSerializer.Serialize(books, Json);
    }

    [McpServerTool(Name = "get_book_details"),
     Description("Întoarce detaliile unei cărți după ID: titlu, autor, gen, an, preț, descriere, edituri, " +
                 "numărul de comenzi și media evaluărilor. ID-ul se află cu search_books.")]
    public static async Task<string> GetBookDetails(
        LibraryContext db,
        [Description("ID-ul cărții din catalog.")] int id)
    {
        var book = await db.Book.AsNoTracking()
            .Where(b => b.ID == id)
            .Select(b => new
            {
                b.ID,
                b.Title,
                Author = b.Author != null ? b.Author.FirstName + " " + b.Author.LastName : "",
                Genre = b.Genre != null ? b.Genre.Name : "",
                b.Year,
                b.Price,
                b.Description,
                Publishers = b.Publishers.Select(p => p.Name).ToList(),
                Orders = b.Orders.Count,
                AverageRating = db.Rating.Where(r => r.BookID == b.ID).Average(r => (double?)r.Stars)
            })
            .FirstOrDefaultAsync();
        return book is null ? $"Nu există nicio carte cu ID-ul {id}." : JsonSerializer.Serialize(book, Json);
    }

    [McpServerTool(Name = "recommend_books"),
     Description("Recomandă cărți unui cititor, după ID-ul lui, folosind modelul ML al Bibliotecii. " +
                 "Pentru un cititor nou (fără evaluări) întoarce cele mai bine notate cărți, cu fallback = true.")]
    public static async Task<string> RecommendBooks(
        RecommendationService recommendations,
        [Description("ID-ul cititorului.")] int readerId,
        [Description("Câte recomandări să întoarcă (implicit 5).")] int top = 5)
    {
        var list = await recommendations.RecommendAsync(readerId, top);
        return JsonSerializer.Serialize(list, Json);
    }

    // Misiunea 3 · tool-ul pentru întrebarea „Care este cea mai scumpă carte din catalog?”
    [McpServerTool(Name = "top_books_by_price"),
     Description("Întoarce cele mai scumpe cărți din catalog, ordonate descrescător după preț.")]
    public static async Task<string> TopBooksByPrice(
        LibraryContext db,
        [Description("Câte cărți să întoarcă (implicit 5).")] int top = 5)
    {
        var books = await db.Book.AsNoTracking()
            .OrderByDescending(b => b.Price)
            .Take(Math.Clamp(top, 1, 20))
            .Select(b => new { b.ID, b.Title, b.Price })
            .ToListAsync();
        return JsonSerializer.Serialize(books, Json);
    }

    // Misiunea 3 · tool-ul scris de noi: cărțile unui gen
    [McpServerTool(Name = "books_by_genre"),
     Description("Întoarce cărțile dintr-un gen literar (de exemplu SF, Fantasy, Poezie, Polițist, Roman românesc), " +
                 "cu ID, titlu, autor, an și preț. Folosește-l pentru întrebări de tipul „ce cărți SF aveți?”.")]
    public static async Task<string> BooksByGenre(
        LibraryContext db,
        [Description("Numele genului sau o parte din el, de exemplu „SF” sau „Poezie”.")] string genre)
    {
        var pattern = $"%{genre}%";
        var books = await db.Book.AsNoTracking()
            .Where(b => b.Genre != null && EF.Functions.Like(b.Genre.Name, pattern))
            .OrderBy(b => b.Title)
            .Select(b => new
            {
                b.ID,
                b.Title,
                Author = b.Author != null ? b.Author.FirstName + " " + b.Author.LastName : "",
                Genre = b.Genre!.Name,
                b.Year,
                b.Price
            })
            .ToListAsync();
        return books.Count == 0
            ? $"Nu există cărți în genul „{genre}”."
            : JsonSerializer.Serialize(books, Json);
    }

    // Î9-S2 · varianta B1: cărțile apărute într-un interval de ani
    [McpServerTool(Name = "books_by_year", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false),
     Description("Întoarce cărțile din catalog apărute într-un interval de ani (inclusiv capetele), " +
                 "ordonate după an. Pentru fiecare carte: ID, titlu, autor, an și preț în lei. Cel mult 10 cărți.")]
    public static async Task<List<BookByYear>> BooksByYear(
        LibraryContext db,
        [Description("Primul an din interval, de exemplu 1920.")] int fromYear,
        [Description("Ultimul an din interval, de exemplu 1939. Trebuie să fie mai mare sau egal cu fromYear.")] int toYear,
        [Description("Câte cărți să întoarcă, între 1 și 10 (implicit 10).")] int top = 10,
        CancellationToken ct = default)
    {
        if (fromYear > toYear)
            throw new McpException(
                $"Intervalul e inversat: fromYear ({fromYear}) e mai mare decât toYear ({toYear}). " +
                $"Apelează din nou cu fromYear = {toYear} și toYear = {fromYear}.");
        if (top is < 1 or > 10)
            throw new McpException($"top trebuie să fie între 1 și 10 (ai trimis {top}).");

        var books = await db.Book.AsNoTracking()
            .Where(b => b.Year >= fromYear && b.Year <= toYear)
            .OrderBy(b => b.Year).ThenBy(b => b.Title)
            .Take(top)
            .Select(b => new BookByYear(b.ID, b.Title,
                b.Author != null ? b.Author.FirstName + " " + b.Author.LastName : "",
                b.Year, b.Price))
            .ToListAsync(ct);

        if (books.Count == 0)
        {
            var minYear = await db.Book.MinAsync(b => b.Year, ct);
            var maxYear = await db.Book.MaxAsync(b => b.Year, ct);
            throw new McpException(
                $"Nu există cărți apărute între {fromYear} și {toYear}. " +
                $"Catalogul are cărți apărute între {minYear} și {maxYear}; alege un interval din acest domeniu.");
        }
        return books;
    }
}

// rezultatul lui books_by_year: un record mic pentru fiecare carte
public record BookByYear(int Id, string Title, string Author, int Year, decimal Price);
