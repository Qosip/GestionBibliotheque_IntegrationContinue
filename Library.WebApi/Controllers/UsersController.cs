using System;
using System.Linq;
using Library.Application.Commands;
using Library.Application.Handlers;
using Library.Infrastructure;
using Library.WebApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace Library.WebApi.Controllers;

public class UsersController : Controller
{
    private readonly RegisterUserHandler _handler;
    private readonly LibraryDbContext _db;

    public UsersController(RegisterUserHandler handler, LibraryDbContext db)
    {
        _handler = handler;
        _db = db;
    }

    // ===== Liste des utilisateurs =====

    [HttpGet]
    public IActionResult Index()
    {
        var users = _db.UserAccounts
            .OrderBy(u => u.Name)
            .ToList();

        return View(users);
    }

    // ===== Détail d'un utilisateur : ses emprunts =====

    [HttpGet]
    public IActionResult Details(Guid id)
    {
        var user = _db.UserAccounts.FirstOrDefault(u => u.Id == id);
        if (user is null)
            return NotFound();

        var loansQuery =
            from loan in _db.Loans
            where loan.UserAccountId == id
            join copy in _db.BookCopies on loan.BookCopyId equals copy.Id
            join book in _db.Books on copy.BookId equals book.Id
            join site in _db.Sites on copy.SiteId equals site.Id
            orderby loan.BorrowedAt descending
            select new UserLoanInfo
            {
                LoanId = loan.Id,
                BookTitle = book.Title,
                SiteName = site.Name,
                BorrowedAt = loan.BorrowedAt,
                DueDate = loan.DueDate,
                ReturnedAt = loan.ReturnedAt
            };

        var vm = new UserDetailsViewModel
        {
            UserId = user.Id,
            Name = user.Name,
            ActiveLoansCount = user.ActiveLoansCount,
            AmountDue = user.AmountDue,
            Loans = loansQuery.ToList()
        };

        return View(vm);
    }

    // ===== Création d'utilisateur (inchangé) =====

    [HttpGet]
    public IActionResult Create()
    {
        return View(new RegisterUserCommand());
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
