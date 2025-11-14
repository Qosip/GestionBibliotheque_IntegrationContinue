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

    // --- Création de livre ---

    [HttpGet]
    public IActionResult Create()
    {
        return View(new RegisterBookCommand(string.Empty, string.Empty, string.Empty));
    }

    [HttpPost]
    public IActionResult Create(RegisterBookCommand command)
    {
        var result = _registerBookHandler.Handle(command);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorCode ?? "Erreur inconnue");
            return View(command);
        }

        ViewData["BookId"] = result.BookId;
        return View("CreateSuccess", result);
    }

    // --- Ajout d'exemplaire avec listes déroulantes ---

    [HttpGet]
    public IActionResult AddCopy()
    {
        var vm = new AddBookCopyViewModel
        {
            Books = _db.Books
                .Select(b => new SelectListItem
                {
                    Value = b.Id.ToString(),
                    Text = b.Title
                })
                .ToList(),

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
    public IActionResult AddCopy(AddBookCopyViewModel model)
    {
        if (!ModelState.IsValid)
        {
            // Recharger les listes si besoin
            model.Books = _db.Books
                .Select(b => new SelectListItem
                {
                    Value = b.Id.ToString(),
                    Text = b.Title
                })
                .ToList();

            model.Sites = _db.Sites
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name
                })
                .ToList();

            return View(model);
        }

        var command = new AddBookCopyCommand(model.BookId, model.SiteId);
        var result = _addBookCopyHandler.Handle(command);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorCode ?? "Erreur inconnue");

            model.Books = _db.Books
                .Select(b => new SelectListItem
                {
                    Value = b.Id.ToString(),
                    Text = b.Title
                })
                .ToList();

            model.Sites = _db.Sites
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name
                })
                .ToList();

            return View(model);
        }

        ViewData["BookCopyId"] = result.BookCopyId;
        return View("AddCopySuccess", result);
    }
}
