namespace Intalnirea1.Models;

public class Genre
{
    public int ID { get; set; }
    public string Name { get; set; } = "";
    public ICollection<Book> Books { get; set; } = [];
}
