// Services/ReaderRatingsGrpcService.cs
// Î9-S1: evaluările unui cititor (listare, citire, adăugare, modificare, ștergere) prin gRPC.
using BibliotecaGrpc;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Intalnirea1.Data;
using Intalnirea1.Models;

namespace Intalnirea1.Services;

public class ReaderRatingsGrpcService(LibraryContext db) : ReaderRatings.ReaderRatingsBase
{
    public override async Task ListRatings(ReaderRequest request,
        IServerStreamWriter<RatingItem> responseStream, ServerCallContext context)
    {
        ValidateReader(request.ReaderId);

        var ratings = await db.Rating.AsNoTracking()
            .Where(r => r.ReaderId == request.ReaderId)
            .OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.ID)
            .Select(r => new { r.ID, r.BookID, r.Book.Title, r.Stars, r.CreatedAt })
            .ToListAsync(context.CancellationToken);

        foreach (var r in ratings)
            await responseStream.WriteAsync(ToItem(r.ID, r.BookID, r.Title, r.Stars, r.CreatedAt),
                context.CancellationToken);
    }

    public override async Task<RatingItem> GetRating(RatingId request, ServerCallContext context)
    {
        var r = await FindAsync(request.Id, context.CancellationToken);
        return ToItem(r.ID, r.BookID, r.Book.Title, r.Stars, r.CreatedAt);
    }

    public override async Task<RatingItem> AddRating(NewRating request, ServerCallContext context)
    {
        ValidateReader(request.ReaderId);
        ValidateStars(request.Stars);

        var book = await db.Book.AsNoTracking()
            .FirstOrDefaultAsync(b => b.ID == request.BookId, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound,
                $"Cartea cu ID-ul {request.BookId} nu există în catalog."));

        var rating = new Rating
        {
            ReaderId = request.ReaderId,
            BookID = request.BookId,
            Stars = request.Stars,
            CreatedAt = DateTime.UtcNow
        };
        db.Rating.Add(rating);
        await db.SaveChangesAsync(context.CancellationToken);

        return ToItem(rating.ID, book.ID, book.Title, rating.Stars, rating.CreatedAt);
    }

    public override async Task<RatingItem> UpdateRating(UpdateRatingRequest request, ServerCallContext context)
    {
        ValidateStars(request.Stars);

        var r = await FindAsync(request.Id, context.CancellationToken, tracking: true);
        r.Stars = request.Stars;
        await db.SaveChangesAsync(context.CancellationToken);

        return ToItem(r.ID, r.BookID, r.Book.Title, r.Stars, r.CreatedAt);
    }

    public override async Task<Empty> DeleteRating(RatingId request, ServerCallContext context)
    {
        var r = await FindAsync(request.Id, context.CancellationToken, tracking: true);
        db.Rating.Remove(r);
        await db.SaveChangesAsync(context.CancellationToken);
        return new Empty();
    }

    private async Task<Rating> FindAsync(int id, CancellationToken ct, bool tracking = false)
    {
        var query = tracking ? db.Rating : db.Rating.AsNoTracking();
        return await query.Include(r => r.Book).FirstOrDefaultAsync(r => r.ID == id, ct)
            ?? throw new RpcException(new Status(StatusCode.NotFound,
                $"Evaluarea cu ID-ul {id} nu există."));
    }

    private static void ValidateReader(int readerId)
    {
        if (readerId <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument,
                "reader_id trebuie să fie un număr pozitiv."));
    }

    private static void ValidateStars(int stars)
    {
        if (stars is < 1 or > 5)
            throw new RpcException(new Status(StatusCode.InvalidArgument,
                $"Nota trebuie să fie între 1 și 5 stele (ai trimis {stars})."));
    }

    // datele din SQLite vin fără Kind; Timestamp acceptă doar DateTime în UTC
    private static RatingItem ToItem(int id, int bookId, string title, int stars, DateTime createdAt) => new()
    {
        Id = id,
        BookId = bookId,
        Title = title,
        Stars = stars,
        CreatedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(createdAt, DateTimeKind.Utc))
    };
}
