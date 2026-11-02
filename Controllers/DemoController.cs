using Microsoft.AspNetCore.Mvc;

namespace Intalnirea1.Controllers;

public class DemoController : Controller
{
    [Route("hello")]
    public string Hello() => "Salut!";

    [HttpGet("hello/{name}")]
    public string HelloByName(string name) => $"Salut, {name}!";

    [HttpGet("calc/{a:int}/{b:int}")]
    public string Calculate(int a, int b) => $"Suma este: {a + b}";

    [HttpGet("carti/{an:int}")]
    public string Carti(int an) => $"Cărți apărute în {an}";
}
