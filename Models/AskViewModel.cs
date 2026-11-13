using System.ComponentModel.DataAnnotations;
using Intalnirea1.Rag;

namespace Intalnirea1.Models;

public class AskViewModel
{
    [Display(Name = "Întrebarea")]
    public string Question { get; set; } = "";

    public string? Answer { get; set; }
    public List<DocumentHit> Sources { get; set; } = [];
    public long Milliseconds { get; set; }
}
