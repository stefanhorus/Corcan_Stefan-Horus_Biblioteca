using System.ComponentModel.DataAnnotations;
using BibliotecaGrpc;

namespace Biblioteca.Portal.Models;

public class RatingsIndexViewModel
{
    [Display(Name = "ID cititor")]
    public int? ReaderId { get; set; }

    public List<RatingItem> Ratings { get; set; } = [];   // tipul generat din ratings.proto
    public string? Error { get; set; }
}

public class RatingFormViewModel
{
    public int Id { get; set; }

    [Display(Name = "ID cititor")]
    public int ReaderId { get; set; }

    [Display(Name = "ID carte")]
    public int BookId { get; set; }

    [Display(Name = "Carte")]
    public string Title { get; set; } = "";

    [Display(Name = "Nota (1–5 stele)")]
    public int Stars { get; set; } = 5;

    public string? Error { get; set; }
}
