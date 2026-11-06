namespace Intalnirea1.Models;

public record StarCount(int Stars, int Count);
public record AuthorAverage(string Author, double Average, int Count);

public record DashboardViewModel(int Ratings, int Readers, double Average,
    List<StarCount> Distribution, List<AuthorAverage> ByAuthor);
