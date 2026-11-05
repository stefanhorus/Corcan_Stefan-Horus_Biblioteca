using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Intalnirea1.Data;
using Intalnirea1.Models;

namespace Intalnirea1.Controllers
{
    public class OrdersController : Controller
    {
        private readonly LibraryContext _context;

        public OrdersController(LibraryContext context)
        {
            _context = context;
        }

        // GET: Orders?customerId=5
        public async Task<IActionResult> Index(int? customerId)
        {
            var orders = _context.Order
                .Include(o => o.Book)
                .Include(o => o.Customer)
                .AsNoTracking();
            if (customerId != null)
                orders = orders.Where(o => o.CustomerID == customerId);

            ViewData["CustomerFilter"] = new SelectList(
                _context.Customer.OrderBy(c => c.Name), "CustomerID", "Name", customerId);
            return View(await orders.OrderByDescending(o => o.OrderDate).ToListAsync());
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Order
                .Include(o => o.Book)
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(m => m.OrderID == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // GET: Orders/Create
        public IActionResult Create()
        {
            PopulateDropdowns();
            return View(new Order { OrderDate = DateTime.Today });
        }

        // POST: Orders/Create
        // Overposting: TryUpdateModelAsync leagă doar CustomerID, BookID și OrderDate.
        [HttpPost, ActionName("Create"), ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePost()
        {
            var order = new Order();
            if (await TryUpdateModelAsync(order, "",
                    o => o.CustomerID, o => o.BookID, o => o.OrderDate))
            {
                _context.Add(order);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            PopulateDropdowns(order);
            return View(order);
        }

        // GET: Orders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Order.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            PopulateDropdowns(order);
            return View(order);
        }

        // POST: Orders/Edit/5
        // Overposting: comanda se citește din baza de date și se modifică doar câmpurile permise.
        [HttpPost, ActionName("Edit"), ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(int? id)
        {
            if (id == null) return NotFound();
            var order = await _context.Order.FirstOrDefaultAsync(o => o.OrderID == id);
            if (order == null) return NotFound();

            if (await TryUpdateModelAsync(order, "",
                    o => o.CustomerID, o => o.BookID, o => o.OrderDate))
            {
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            PopulateDropdowns(order);
            return View(order);
        }

        private void PopulateDropdowns(Order? order = null)
        {
            ViewData["CustomerID"] = new SelectList(
                _context.Customer.OrderBy(c => c.Name), "CustomerID", "Name", order?.CustomerID);
            ViewData["BookID"] = new SelectList(
                _context.Book.OrderBy(b => b.Title), "ID", "Title", order?.BookID);
        }

        // GET: Orders/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Order
                .Include(o => o.Book)
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(m => m.OrderID == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Order.FindAsync(id);
            if (order != null)
            {
                _context.Order.Remove(order);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OrderExists(int id)
        {
            return _context.Order.Any(e => e.OrderID == id);
        }
    }
}
