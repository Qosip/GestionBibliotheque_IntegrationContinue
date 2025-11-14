using Microsoft.AspNetCore.Mvc;

namespace Library.WebApi.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
