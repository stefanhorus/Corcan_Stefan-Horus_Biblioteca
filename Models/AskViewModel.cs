using System.ComponentModel.DataAnnotations;
using Intalnirea1.Rag;

namespace Intalnirea1.Models;

public class AskViewModel
{
    [Display(Name = "Întrebarea")]
    public string Question { get; set; } = "";

    // Î10-S2: documentul în care se caută ("regulament", "cont-online" ...)
    [Display(Name = "Documentul")]
    public string Source { get; set; } = "regulament";

    public List<string> Documents { get; set; } = [];

    public string? Answer { get; set; }
    public List<DocumentHit> Sources { get; set; } = [];
    public long Milliseconds { get; set; }
}
