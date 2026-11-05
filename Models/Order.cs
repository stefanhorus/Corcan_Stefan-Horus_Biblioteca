using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Intalnirea1.Models;

public class Order                            // n-n CU date proprii
{
    public int OrderID { get; set; }
    [Display(Name = "Cititor")] public int CustomerID { get; set; }
    [Display(Name = "Carte")] public int BookID { get; set; }
    [Display(Name = "Data")] public DateTime OrderDate { get; set; }

    // Formularul trimite doar ID-urile; fără [ValidateNever], validarea ar cere
    // și obiectele de navigare (tipuri non-nullable) și Create/Edit n-ar salva nimic.
    [ValidateNever, Display(Name = "Cititor")] public Customer Customer { get; set; } = null!;
    [ValidateNever, Display(Name = "Carte")] public Book Book { get; set; } = null!;
}
