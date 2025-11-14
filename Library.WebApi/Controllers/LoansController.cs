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

        // Sites disponibles
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

            // Livres disponibles sur le site
            var booksQuery =
                from copy in _db.BookCopies
                join book in _db.Books on copy.BookId equals book.Id
                where copy.SiteId == siteId.Value && copy.Status == BookCopyStatus.Available
                select new { book.Id, book.Title };

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

            // Recharger les listes
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

            var books = booksQuery
                .AsEnumerable()
                .GroupBy(b => b.Id)
                .Select(g => g.First())
                .ToList();

            model.Books = books
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

    // ===== Retour =====

    [HttpGet]
    public IActionResult Return(Guid? userId)
    {
        var vm = new ReturnViewModel();

        // Liste des utilisateurs
        vm.Users = _db.UserAccounts
            .OrderBy(u => u.Name)
            .Select(u => new SelectListItem
            {
                Value = u.Id.ToString(),
                Text = u.Name
            })
            .ToList();

        if (userId.HasValue && userId.Value != Guid.Empty)
        {
            vm.UserId = userId.Value;

            // Prêts en cours pour cet utilisateur
            var loansQuery =
                from loan in _db.Loans
                where loan.UserAccountId == userId.Value && loan.ReturnedAt == null
                join copy in _db.BookCopies on loan.BookCopyId equals copy.Id
                join book in _db.Books on copy.BookId equals book.Id
                join site in _db.Sites on copy.SiteId equals site.Id
                orderby loan.BorrowedAt descending
                select new
                {
                    loan.Id,
                    BookTitle = book.Title,
                    SiteName = site.Name
                };

            var loans = loansQuery.ToList();

            vm.Loans = loans
                .Select(l => new SelectListItem
                {
                    Value = l.Id.ToString(),
                    Text = $"{l.BookTitle} ({l.SiteName})"
                })
                .ToList();
        }
        else
        {
            vm.UserId = Guid.Empty;
            vm.Loans = Enumerable.Empty<SelectListItem>();
        }

        return View(vm);
    }

    [HttpPost]
    public IActionResult Return(ReturnViewModel model)
    {
        var command = new ReturnBookCommand(model.LoanId);
        var result = _returnHandler.Handle(command);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorCode ?? "Erreur inconnue");

            // Recharger les listes
            model.Users = _db.UserAccounts
                .OrderBy(u => u.Name)
                .Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = u.Name
                })
                .ToList();

            var loansQuery =
                from loan in _db.Loans
                where loan.UserAccountId == model.UserId && loan.ReturnedAt == null
                join copy in _db.BookCopies on loan.BookCopyId equals copy.Id
                join book in _db.Books on copy.BookId equals book.Id
                join site in _db.Sites on copy.SiteId equals site.Id
                orderby loan.BorrowedAt descending
                select new
                {
                    loan.Id,
                    BookTitle = book.Title,
                    SiteName = site.Name
                };

            var loans = loansQuery.ToList();

            model.Loans = loans
                .Select(l => new SelectListItem
                {
                    Value = l.Id.ToString(),
                    Text = $"{l.BookTitle} ({l.SiteName})"
                })
                .ToList();

            return View(model);
        }

        return View("ReturnSuccess", result);
    }
}
