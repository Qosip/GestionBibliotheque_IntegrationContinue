using System;
using System.Linq;
using Library.Application;
using Library.Infrastructure;
using Library.WebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Library.WebApi.Controllers;

public class TransfersController : Controller
{
    private readonly RequestTransferHandler _handler;
    private readonly LibraryDbContext _db;

    public TransfersController(RequestTransferHandler handler, LibraryDbContext db)
    {
        _handler = handler;
        _db = db;
    }

    [HttpGet]
    public IActionResult Request()
    {
        var vm = new TransferRequestViewModel
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
    public IActionResult Request(TransferRequestViewModel model)
    {
        var command = new RequestTransferCommand(
            model.BookId,
            model.SourceSiteId,
            model.TargetSiteId);

        var result = _handler.Handle(command);

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
        return View("RequestSuccess", result);
    }
}
