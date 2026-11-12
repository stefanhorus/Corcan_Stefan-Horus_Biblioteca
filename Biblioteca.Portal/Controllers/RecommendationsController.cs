using Biblioteca.Portal.Models;
using BibliotecaGrpc;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;

namespace Biblioteca.Portal.Controllers;

public class RecommendationsController(Recommender.RecommenderClient client) : Controller
{
    [HttpGet]
    public IActionResult Index() => View(new RecommendationsViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(RecommendationsViewModel vm)
    {
        try
        {
            var reply = await client.RecommendAsync(
                new RecommendRequest { ReaderId = vm.ReaderId, Top = vm.Top },
                deadline: DateTime.UtcNow.AddSeconds(5));
            vm.Books = reply.Books.ToList();
        }
        catch (RpcException ex)
        {
            vm.Error = $"{ex.StatusCode}: {ex.Status.Detail}";
        }
        return View(vm);
    }
}
