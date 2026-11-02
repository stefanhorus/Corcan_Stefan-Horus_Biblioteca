using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Intalnirea1.Models;

namespace Intalnirea1.Data
{
    public class LibraryContext : DbContext
    {
        public LibraryContext (DbContextOptions<LibraryContext> options)
            : base(options)
        {
        }

        public DbSet<Intalnirea1.Models.Book> Book { get; set; } = default!;
        public DbSet<Intalnirea1.Models.Author> Author { get; set; } = default!;
    }
}
