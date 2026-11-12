// Services/RecommenderGrpcService.cs
// Implementarea serviciului din recommender.proto.
// Recommender.RecommenderBase este generata la build din fisierul .proto (folderul obj).
using BibliotecaGrpc;
using Grpc.Core;
using Intalnirea1.Data;
using Intalnirea1.Models;

namespace Intalnirea1.Services;

public class RecommenderGrpcService(
    RecommendationService recs,
    LibraryContext db,
    ILogger<RecommenderGrpcService> logger) : Recommender.RecommenderBase
{
   
    public override async Task<RecommendReply> Recommend(
        RecommendRequest request, ServerCallContext context)
    {
        ValidateRequest(request);
        var list = await recs.RecommendAsync(
            request.ReaderId, request.Top, context.CancellationToken);

        var reply = new RecommendReply();
        reply.Books.AddRange(list.Select(ToMessage));   
        return reply;
    }

    
    public override async Task StreamRecommendations(
        RecommendRequest request,
        IServerStreamWriter<BookScore> responseStream,
        ServerCallContext context)
    {
        ValidateRequest(request);
        var list = await recs.RecommendAsync(
            request.ReaderId, request.Top, context.CancellationToken);

        foreach (var r in list)
        {
            await responseStream.WriteAsync(ToMessage(r));
            // simulăm un calcul lent (de ex. un model mai mare)
            await Task.Delay(500, context.CancellationToken);
        }
    }

  
    public override async Task<RateSummary> RateBooks(
        IAsyncStreamReader<RatingInput> requestStream, ServerCallContext context)
    {
        int saved = 0, rejected = 0;
        double sum = 0;

        await foreach (var r in requestStream.ReadAllAsync(context.CancellationToken))
        {
            if (r.Stars is < 1 or > 5) { rejected++; continue; }

            db.Rating.Add(new Rating
            {
                ReaderId = r.ReaderId,
                BookID = r.BookId,
                Stars = r.Stars,
                CreatedAt = DateTime.UtcNow
            });
            saved++;
            sum += r.Stars;
        }

        await db.SaveChangesAsync(context.CancellationToken);
        logger.LogInformation("RateBooks: {Saved} salvate, {Rejected} respinse", saved, rejected);

        return new RateSummary
        {
            Saved = saved,
            Rejected = rejected,
            Average = saved == 0 ? 0 : sum / saved
        };
    }

    // Î9-A3: cererile invalide primesc InvalidArgument, cu un mesaj care spune ce e greșit
    private static void ValidateRequest(RecommendRequest request)
    {
        if (request.ReaderId <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument,
                "reader_id trebuie să fie un număr pozitiv."));
        if (request.Top > 20)
            throw new RpcException(new Status(StatusCode.InvalidArgument,
                "top poate fi cel mult 20."));
    }

    private static BookScore ToMessage(Recommendation r) => new()
    {
        BookId = r.BookId,
        Title = r.Title,
        Score = r.Score,
        Fallback = r.Fallback,
        Label = Eticheta(r)
    };

    // [V] pragurile variantei A0
    private const float PragFoarte = 4.0f;
    private const float PragRecomandata = 3.0f;

    private static string Eticheta(Recommendation r) =>
        r.Fallback ? "Populară (cititor nou)"
        : r.Score >= PragFoarte ? "Foarte recomandată"
        : r.Score >= PragRecomandata ? "Recomandată"
        : "Neutră";
}
