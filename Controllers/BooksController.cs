using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Intalnirea1.Data;
using Intalnirea1.Models;

namespace Intalnirea1.Controllers
{
    public class BooksController : Controller
    {
        private readonly LibraryContext _context;
        private readonly ILogger<BooksController> _logger;

        public BooksController(LibraryContext context, ILogger<BooksController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Books?v=A|B|C|D  (cele patru variante din fișă; implicit D)
        public async Task<IActionResult> Index(string v = "D")
        {
            var sw = Stopwatch.StartNew();
            List<BookRow> rows;

            switch (v.ToUpperInvariant())
            {
                case "A": // fără Include
                {
                    var books = await _context.Book.ToListAsync();
                    rows = ToRows(books);
                    break;
                }
                case "B": // eager loading
                {
                    var books = await _context.Book
                        .Include(b => b.Author).Include(b => b.Orders)
                        .ToListAsync();
                    rows = ToRows(books);
                    break;
                }
                case "C": // încărcare explicită în buclă
                {
                    var books = await _context.Book.ToListAsync();
                    foreach (var b in books)
                    {
                        var e = _context.Entry(b);
                        await e.Reference(x => x.Author).LoadAsync();
                        await e.Collection(x => x.Orders).LoadAsync();
                    }
                    rows = ToRows(books);
                    break;
                }
                default: // D · proiecție
                    v = "D";
                    rows = await _context.Book
                        .Select(b => new BookRow(b.Title,
                            b.Author!.FirstName + " " + b.Author.LastName,
                            b.Orders.Count))
                        .ToListAsync();
                    break;
            }

            _logger.LogInformation("Index {Variant}: {Ms} ms", v, sw.ElapsedMilliseconds);
            ViewData["Variant"] = v;
            return View(rows);
        }

        private static List<BookRow> ToRows(List<Book> books) =>
            books.Select(b => new BookRow(b.Title,
                b.Author?.LastName ?? "(gol)", b.Orders.Count)).ToList();

        // GET: Books/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Book
                .Include(b => b.Genre)
                .Include(b => b.Author)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        // GET: Books/Create
        public IActionResult Create()
        {
            ViewData["GenreID"] = new SelectList(_context.Set<Genre>(), "ID", "ID");
            ViewData["AuthorID"] = new SelectList(_context.Set<Author>(), "ID", "FullName");
            return View();
        }

        // POST: Books/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,Title,Year,Price,Description,AuthorID,GenreID")] Book book)
        {
            if (ModelState.IsValid)
            {
                _context.Add(book);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["GenreID"] = new SelectList(_context.Set<Genre>(), "ID", "ID", book.GenreID);
            ViewData["AuthorID"] = new SelectList(_context.Set<Author>(), "ID", "FullName", book.AuthorID);
            return View(book);
        }

        // GET: Books/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Book.FindAsync(id);
            if (book == null)
            {
                return NotFound();
            }
            ViewData["GenreID"] = new SelectList(_context.Set<Genre>(), "ID", "ID", book.GenreID);
            ViewData["AuthorID"] = new SelectList(_context.Set<Author>(), "ID", "FullName", book.AuthorID);
            return View(new BookEditViewModel
            {
                ID = book.ID, Title = book.Title, Price = book.Price,
                AuthorID = book.AuthorID, GenreID = book.GenreID,
            });
        }

        // POST: Books/Edit/5
        // Apărarea 2 · ViewModel
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BookEditViewModel vm)
        {
            if (id != vm.ID) return NotFound();
            if (!ModelState.IsValid)
            {
                ViewData["GenreID"] = new SelectList(_context.Set<Genre>(), "ID", "ID", vm.GenreID);
                ViewData["AuthorID"] = new SelectList(_context.Set<Author>(), "ID", "FullName", vm.AuthorID);
                return View(vm);
            }
            var book = await _context.Book.FindAsync(id);
            if (book is null) return NotFound();
            book.Title = vm.Title; book.Price = vm.Price;
            book.AuthorID = vm.AuthorID; book.GenreID = vm.GenreID;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Books/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Book
                .Include(b => b.Genre)
                .Include(b => b.Author)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        // POST: Books/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var book = await _context.Book.FindAsync(id);
            if (book != null)
            {
                _context.Book.Remove(book);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool BookExists(int id)
        {
            return _context.Book.Any(e => e.ID == id);
        }
    }
}
