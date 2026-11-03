namespace Intalnirea1.Models;

public class Order                            // n-n CU date proprii
{
    public int OrderID { get; set; }
    public int CustomerID { get; set; }
    public int BookID { get; set; }
    public DateTime OrderDate { get; set; }
    public Customer Customer { get; set; } = null!;
    public Book Book { get; set; } = null!;
}
