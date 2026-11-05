namespace Intalnirea1.Models;

public record AuthorBookRow(int ID, string Title, string Genre, decimal Price, int Orders);

public record AuthorDetailsViewModel(Author Author, List<AuthorBookRow> Books,
    int TotalOrders, decimal? AveragePrice);
