namespace Intalnirea1.Models;

public class Publisher                        // n-n FĂRĂ date proprii
{
    public int ID { get; set; }
    public string Name { get; set; } = "";
    public ICollection<Book> Books { get; set; } = [];
}
