using System.ComponentModel.DataAnnotations;

namespace Intalnirea1.Models;

public class RecommendationLog
{
    public int ID { get; set; }
    [Display(Name = "Cititor")] public int ReaderId { get; set; }
    [Display(Name = "Data (UTC)")] public DateTime CreatedAt { get; set; }
    [Display(Name = "Cărți întoarse")] public string BookIds { get; set; } = "";   // ID-urile, separate prin virgulă, în ordinea întoarsă
    [Display(Name = "Cold start")] public bool Fallback { get; set; }
}
