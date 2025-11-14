using Library.Application;
using Microsoft.AspNetCore.Mvc;

namespace Library.WebApi.Controllers;

public class SitesController : Controller
{
    private readonly CreateSiteHandler _handler;

    public SitesController(CreateSiteHandler handler)
    {
        _handler = handler;
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateSiteCommand(string.Empty, string.Empty));
    }

    [HttpPost]
    public IActionResult Create(CreateSiteCommand command)
    {
        var result = _handler.Handle(command);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorCode ?? "Erreur inconnue");
            return View(command);
        }

        ViewBag.SiteId = result.SiteId;
        return View("CreateSuccess", result);
    }
}
