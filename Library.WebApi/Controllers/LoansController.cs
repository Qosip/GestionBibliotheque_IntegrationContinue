using System;
using System.Linq;
using Library.Application;
using Library.Infrastructure;
using Library.WebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Library.WebApi.Controllers;

public class LoansController : Controller
{
    private readonly BorrowBookHandler _borrowHandler;
    private readonly ReturnBookHandler _returnHandler;
    private readonly LibraryDbContext _db;

    public LoansController(
        BorrowBookHandler borrowHandler,
        ReturnBookHandler returnHandler,
        LibraryDbContext db)
    {
        _borrowHandler = borrowHandler;
        _returnHandler = returnHandler;
        _db = db;
    }

    [HttpGet]
    public IActionResult Borrow()
    {
        var vm = new BorrowViewModel
        {
            Users = _db.UserAccounts
                .Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = u.Id.ToString() // idéalement: u.Name si tu ajoutes la propriété
                })
                .ToList(),

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
    public IActionResult Borrow(BorrowViewModel model)
    {
        if (!ModelState.IsValid)
        {
            // on recharge les listes en cas d’erreur de validation
            model.Users = _db.UserAccounts
                .Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = u.Id.ToString()
                })
                .ToList();

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

        var command = new BorrowBookCommand(model.UserId, model.BookId, model.SiteId);
        var result = _borrowHandler.Handle(command);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorCode ?? "Erreur inconnue");

            // recharger les listes pour réafficher le formulaire avec l’erreur
            model.Users = _db.UserAccounts
                .Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = u.Id.ToString()
                })
                .ToList();

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

        ViewBag.LoanId = result.LoanId;
        return View("BorrowSuccess", result);
    }

    [HttpGet]
    public IActionResult Return()
    {
        return View(new ReturnBookCommand(Guid.Empty));
    }

    [HttpPost]
    public IActionResult Return(ReturnBookCommand command)
    {
        var result = _returnHandler.Handle(command);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorCode ?? "Erreur inconnue");
            return View(command);
        }

        return View("ReturnSuccess", result);
    }
}
