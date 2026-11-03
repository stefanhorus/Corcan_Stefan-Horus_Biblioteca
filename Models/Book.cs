using Microsoft.EntityFrameworkCore;

namespace Intalnirea1.Models;

public class Book
{
    public int ID { get; set; }
    public string Title { get; set; } = "";
    public int Year { get; set; }
    [Precision(10, 2)]
    public decimal Price { get; set; }
    public string? Description { get; set; }
    public bool IsFeatured { get; set; }

    public int? AuthorID { get; set; }
    public Author? Author { get; set; }
    public int? GenreID { get; set; }
    public Genre? Genre { get; set; }

    public ICollection<Order> Orders { get; set; } = [];
    public ICollection<Publisher> Publishers { get; set; } = [];
}
