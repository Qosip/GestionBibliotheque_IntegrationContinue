using System;
using System.Linq;
using Library.Application;
using Library.Domain;
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

    // ===== Emprunt =====

    [HttpGet]
    public IActionResult Borrow(Guid? siteId)
    {
        var vm = new BorrowViewModel();

        // Liste des sites
        vm.Sites = _db.Sites
            .Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.Name
            })
            .ToList();

        if (siteId.HasValue && siteId.Value != Guid.Empty)
        {
            vm.SiteId = siteId.Value;

            // Utilisateurs par nom
            vm.Users = _db.UserAccounts
                .Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = u.Name
                })
                .ToList();

            // Livres disponibles sur ce site (copies en statut Available)
            var booksQuery =
            from copy in _db.BookCopies
            join book in _db.Books on copy.BookId equals book.Id
            where copy.SiteId == siteId.Value && copy.Status == BookCopyStatus.Available
            select new { book.Id, book.Title };

            // On ramène en mémoire avant le GroupBy
            var books = booksQuery
                .AsEnumerable()
                .GroupBy(b => b.Id)
                .Select(g => g.First())
                .ToList();

            vm.Books = books
                .Select(b => new SelectListItem
                {
                    Value = b.Id.ToString(),
                    Text = b.Title
                })
                .ToList();

        }
        else
        {
            vm.SiteId = Guid.Empty;
            vm.Users = Enumerable.Empty<SelectListItem>();
            vm.Books = Enumerable.Empty<SelectListItem>();
        }

        return View(vm);
    }

    [HttpPost]
    public IActionResult Borrow(BorrowViewModel model)
    {
        var command = new BorrowBookCommand(model.UserId, model.BookId, model.SiteId);
        var result = _borrowHandler.Handle(command);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorCode ?? "Erreur inconnue");

            // recharger les listes pour réafficher la vue
            model.Sites = _db.Sites
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name
                })
                .ToList();

            model.Users = _db.UserAccounts
                .Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = u.Name
                })
                .ToList();

            var booksQuery =
                from copy in _db.BookCopies
                join book in _db.Books on copy.BookId equals book.Id
                where copy.SiteId == model.SiteId && copy.Status == BookCopyStatus.Available
                select new { book.Id, book.Title };

            model.Books = booksQuery
                .GroupBy(b => b.Id)
                .Select(g => g.First())
                .Select(b => new SelectListItem
                {
                    Value = b.Id.ToString(),
                    Text = b.Title
                })
                .ToList();

            return View(model);
        }

        ViewBag.LoanId = result.LoanId;
        return View("BorrowSuccess", result);
    }

    // ===== Retour (laisse simple pour l'instant) =====

    [HttpGet]
    public IActionResult Return()
    {
        return View(new ReturnBookCommand());
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
