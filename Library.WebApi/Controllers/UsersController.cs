using Library.Application;
using Microsoft.AspNetCore.Mvc;

namespace Library.WebApi.Controllers;

public class UsersController : Controller
{
    private readonly RegisterUserHandler _handler;

    public UsersController(RegisterUserHandler handler)
    {
        _handler = handler;
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new RegisterUserCommand(string.Empty));
    }

    [HttpPost]
    public IActionResult Create(RegisterUserCommand command)
    {
        var result = _handler.Handle(command);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorCode ?? "Erreur inconnue");
            return View(command);
        }

        ViewBag.UserId = result.UserId;
        return View("CreateSuccess", result);
    }
}
