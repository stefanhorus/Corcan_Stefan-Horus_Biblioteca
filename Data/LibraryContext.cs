using Microsoft.EntityFrameworkCore;
using Intalnirea1.Models;

namespace Intalnirea1.Data;

public class LibraryContext(DbContextOptions<LibraryContext> options) : DbContext(options)
{
    public DbSet<Book> Book { get; set; } = default!;
    public DbSet<Author> Author { get; set; } = default!;
    public DbSet<Genre> Genre { get; set; } = default!;
    public DbSet<Publisher> Publisher { get; set; } = default!;
    public DbSet<Customer> Customer { get; set; } = default!;
    public DbSet<Order> Order { get; set; } = default!;
    public DbSet<Rating> Rating { get; set; } = default!;
}
