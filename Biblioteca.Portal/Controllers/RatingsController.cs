using Biblioteca.Portal.Models;
using BibliotecaGrpc;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;

namespace Biblioteca.Portal.Controllers;

// Î9-S1: evaluările unui cititor, prin serviciul gRPC ReaderRatings al Bibliotecii.
// Erorile gRPC se afișează în pagină (Model.Error), nu ca pagină de eroare.
public class RatingsController(ReaderRatings.ReaderRatingsClient client) : Controller
{
    private static DateTime Deadline => DateTime.UtcNow.AddSeconds(5);

    // GET: Ratings?readerId=1055 — lista vine din fluxul ListRatings (server streaming)
    public async Task<IActionResult> Index(int? readerId)
    {
        var vm = new RatingsIndexViewModel { ReaderId = readerId };
        if (readerId is null) return View(vm);

        try
        {
            using var call = client.ListRatings(new ReaderRequest { ReaderId = readerId.Value }, deadline: Deadline);
            await foreach (var item in call.ResponseStream.ReadAllAsync())
                vm.Ratings.Add(item);
        }
        catch (RpcException ex)
        {
            vm.Error = Message(ex);
        }
        return View(vm);
    }

    public IActionResult Create(int readerId) => View(new RatingFormViewModel { ReaderId = readerId });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RatingFormViewModel vm)
    {
        try
        {
            await client.AddRatingAsync(
                new NewRating { ReaderId = vm.ReaderId, BookId = vm.BookId, Stars = vm.Stars }, deadline: Deadline);
            return RedirectToAction(nameof(Index), new { readerId = vm.ReaderId });
        }
        catch (RpcException ex)
        {
            vm.Error = Message(ex);
            return View(vm);
        }
    }

    public async Task<IActionResult> Edit(int id, int readerId) => View(await LoadAsync(id, readerId));

    // doar nota se poate modifica
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(RatingFormViewModel vm)
    {
        try
        {
            await client.UpdateRatingAsync(new UpdateRatingRequest { Id = vm.Id, Stars = vm.Stars }, deadline: Deadline);
            return RedirectToAction(nameof(Index), new { readerId = vm.ReaderId });
        }
        catch (RpcException ex)
        {
            vm.Error = Message(ex);
            return View(vm);
        }
    }

    public async Task<IActionResult> Delete(int id, int readerId) => View(await LoadAsync(id, readerId));

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(RatingFormViewModel vm)
    {
        try
        {
            await client.DeleteRatingAsync(new RatingId { Id = vm.Id }, deadline: Deadline);
            return RedirectToAction(nameof(Index), new { readerId = vm.ReaderId });
        }
        catch (RpcException ex)
        {
            vm.Error = Message(ex);
            return View(vm);
        }
    }

    private async Task<RatingFormViewModel> LoadAsync(int id, int readerId)
    {
        var vm = new RatingFormViewModel { Id = id, ReaderId = readerId };
        try
        {
            var r = await client.GetRatingAsync(new RatingId { Id = id }, deadline: Deadline);
            vm.BookId = r.BookId;
            vm.Title = r.Title;
            vm.Stars = r.Stars;
        }
        catch (RpcException ex)
        {
            vm.Error = Message(ex);
        }
        return vm;
    }

    private static string Message(RpcException ex) => $"{ex.StatusCode}: {ex.Status.Detail}";
}
