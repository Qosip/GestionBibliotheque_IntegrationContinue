using System;
using System.Linq;
using Library.Application;
using Library.Infrastructure;
using Library.WebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Library.WebApi.Controllers;

public class BooksController : Controller
{
    private readonly RegisterBookHandler _registerBookHandler;
    private readonly AddBookCopyHandler _addBookCopyHandler;
    private readonly LibraryDbContext _db;

    public BooksController(
        RegisterBookHandler registerBookHandler,
        AddBookCopyHandler addBookCopyHandler,
        LibraryDbContext db)
    {
        _registerBookHandler = registerBookHandler;
        _addBookCopyHandler = addBookCopyHandler;
        _db = db;
    }

    // ===== Création de livre + première copie =====

    [HttpGet]
    public IActionResult Create()
    {
        var vm = new CreateBookWithCopyViewModel
        {
            Sites = _db.Sites
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name
                })
                .ToList()
        };

        return View(vm);
    }

    [HttpPost]
    public IActionResult Create(CreateBookWithCopyViewModel model)
    {
        if (model.SiteId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(model.SiteId), "Un site doit être sélectionné.");
        }

        if (!ModelState.IsValid)
        {
            model.Sites = _db.Sites
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name
                })
                .ToList();

            return View(model);
        }

        var registerCommand = new RegisterBookCommand(model.Isbn, model.Title, model.Author);
        var registerResult = _registerBookHandler.Handle(registerCommand);

        if (!registerResult.Success)
        {
            ModelState.AddModelError(string.Empty, registerResult.ErrorCode ?? "Erreur lors de la création du livre.");

            model.Sites = _db.Sites
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name
                })
                .ToList();

            return View(model);
        }

        var addCopyCommand = new AddBookCopyCommand(registerResult.BookId, model.SiteId);
        var addCopyResult = _addBookCopyHandler.Handle(addCopyCommand);

        if (!addCopyResult.Success)
        {
            ModelState.AddModelError(string.Empty, addCopyResult.ErrorCode ?? "Erreur lors de la création de l'exemplaire.");

            model.Sites = _db.Sites
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name
                })
                .ToList();

            return View(model);
        }

        ViewBag.BookId = registerResult.BookId;
        ViewBag.BookCopyId = addCopyResult.BookCopyId;
        ViewBag.SiteId = model.SiteId;

        return View("CreateSuccess");
    }
}
