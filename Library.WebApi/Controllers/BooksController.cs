using System;
using System.Linq;
using Library.Application.Commands;
using Library.Application.Handlers;
using Library.Application.Repositories;
using Library.Domain;
using Library.Domain.Entities;
using Library.WebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Library.WebApi.Controllers;

public class BooksController : Controller
{
    private readonly RegisterBookHandler _registerBookHandler;
    private readonly AddBookCopyHandler _addBookCopyHandler;

    private readonly IBookRepository _bookRepository;
    private readonly ISiteRepository _siteRepository;
    private readonly IBookCopyRepository _bookCopyRepository;

    // --------------------------------------------------------
    // Constructeur corrigé : injection complète des dépendances
    // --------------------------------------------------------
    public BooksController(
        RegisterBookHandler registerBookHandler,
        AddBookCopyHandler addBookCopyHandler,
        IBookRepository bookRepository,
        ISiteRepository siteRepository,
        IBookCopyRepository bookCopyRepository)
    {
        _registerBookHandler = registerBookHandler
            ?? throw new ArgumentNullException(nameof(registerBookHandler));

        _addBookCopyHandler = addBookCopyHandler
            ?? throw new ArgumentNullException(nameof(addBookCopyHandler));

        _bookRepository = bookRepository
            ?? throw new ArgumentNullException(nameof(bookRepository));

        _siteRepository = siteRepository
            ?? throw new ArgumentNullException(nameof(siteRepository));

        _bookCopyRepository = bookCopyRepository
            ?? throw new ArgumentNullException(nameof(bookCopyRepository));
    }

    // --------------------------------------------------------
    // 1. Création d’un livre + création automatique d’un exemplaire
    // --------------------------------------------------------

    [HttpGet]
    public IActionResult Create()
    {
        var vm = new CreateBookWithCopyViewModel
        {
            Sites = _siteRepository.GetAll()
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
            model.Sites = _siteRepository.GetAll()
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

            model.Sites = _siteRepository.GetAll()
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

            model.Sites = _siteRepository.GetAll()
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

    // --------------------------------------------------------
    // 2. Ajout manuel d’exemplaires supplémentaires
    // --------------------------------------------------------

    // GET /Books/AddCopy?bookId=...
    [HttpGet]
    public IActionResult AddCopy(Guid? bookId)
    {
        var vm = BuildAddCopyViewModel();

        if (bookId.HasValue)
            vm.SelectedBookId = bookId.Value;

        return View(vm);
    }

    // POST /Books/AddCopy
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddCopy(AddBookCopyViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var vm = BuildAddCopyViewModel(model);
            return View(vm);
        }

        for (var i = 0; i < model.Quantity; i++)
        {
            var copy = new BookCopy(model.SelectedBookId, model.SelectedSiteId);
            _bookCopyRepository.Add(copy); // SaveChanges dans le repo
        }

        return RedirectToAction(nameof(AddCopySuccess));
    }


    [HttpGet]
    public IActionResult AddCopySuccess()
    {
        return View();
    }

    // --------------------------------------------------------
    // Helper interne pour les listes
    // --------------------------------------------------------

    private AddBookCopyViewModel BuildAddCopyViewModel(AddBookCopyViewModel? existing = null)
    {
        var books = _bookRepository.GetAll()
            .Select(b => new SelectListItem
            {
                Value = b.Id.ToString(),
                Text = b.Title
            })
            .ToList();

        var sites = _siteRepository.GetAll()
            .Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.Name
            })
            .ToList();

        var vm = existing ?? new AddBookCopyViewModel();
        vm.Books = books;
        vm.Sites = sites;

        return vm;
    }
    [HttpGet]
    public IActionResult Index()
    {
        var books = _bookRepository.GetAll()
            .Select(b => new BookListItemViewModel
            {
                Id = b.Id,
                Isbn = b.Isbn,
                Title = b.Title,
                Author = b.Author
            })
            .ToList();

        return View(books);
    }

}
