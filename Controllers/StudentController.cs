using Microsoft.AspNetCore.Mvc;
using Intalnirea1.Models;

namespace Intalnirea1.Controllers;

public class StudentController : Controller
{
    public IActionResult Info()
    {
        var student = new Student { Name = "Ana", Age = 21 };
        return View(student);
    }
}
