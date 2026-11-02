using System.ComponentModel.DataAnnotations.Schema;

namespace Intalnirea1.Models;

public class Author
{
    public int ID { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public ICollection<Book> Books { get; set; } = [];

    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";
}
