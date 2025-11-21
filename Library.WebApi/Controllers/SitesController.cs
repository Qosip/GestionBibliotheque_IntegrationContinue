using System;
using System.Linq;
using Library.Application.Commands;
using Library.Application.Handlers;
using Library.Domain.Enums;
using Library.Infrastructure;
using Library.WebApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace Library.WebApi.Controllers;

public class SitesController : Controller
{
    private readonly CreateSiteHandler _handler;
    private readonly LibraryDbContext _db;

    public SitesController(CreateSiteHandler handler, LibraryDbContext db)
    {
        _handler = handler;
        _db = db;
    }

    // ===== Liste des sites =====

    [HttpGet]
    public IActionResult Index()
    {
        var sites = _db.Sites
            .OrderBy(s => s.Name)
            .ToList();

        return View(sites);
    }

    // ===== Détail d'un site : livres et statuts des exemplaires =====

    [HttpGet]
    public IActionResult Details(Guid id)
    {
        var site = _db.Sites.FirstOrDefault(s => s.Id == id);
        if (site is null)
            return NotFound();

        var query =
            from copy in _db.BookCopies
            join book in _db.Books on copy.BookId equals book.Id
            where copy.SiteId == id
                && copy.Status != BookCopyStatus.InTransfer
            select new { book.Id, book.Title, copy.Status };

        var grouped = query
            .AsEnumerable() // GroupBy côté mémoire pour éviter les soucis de traduction LINQ
            .GroupBy(x => new { x.Id, x.Title })
            .Select(g => new SiteBookSummary
            {
                BookId = g.Key.Id,
                Title = g.Key.Title,
                TotalCopies = g.Count(),
                AvailableCopies = g.Count(c => c.Status == BookCopyStatus.Available),
                BorrowedCopies = g.Count(c => c.Status == BookCopyStatus.Borrowed),
                InTransferCopies = g.Count(c => c.Status == BookCopyStatus.InTransfer)
            })
            .OrderBy(s => s.Title)
            .ToList();

        var vm = new SiteDetailsViewModel
        {
            SiteId = site.Id,
            Name = site.Name,
            Address = site.Address,
            Books = grouped
        };

        return View(vm);
    }

    // ===== Création de site =====

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateSiteCommand());
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
