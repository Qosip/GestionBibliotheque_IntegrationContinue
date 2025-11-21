using System;
using System.Linq;
using Library.Application.Commands;
using Library.Application.Handlers;
using Library.Application.Results;
using Library.Domain.Enums;
using Library.Infrastructure;
using Library.WebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Library.WebApi.Controllers
{
    public class TransfersController : Controller
    {
        private readonly RequestTransferHandler _handler;
        private readonly LibraryDbContext _db;
        private readonly ReceiveTransferHandler _receiveHandler;


        public TransfersController(RequestTransferHandler handler, ReceiveTransferHandler receiveHandler, LibraryDbContext db)
        {
            _handler = handler;
            _receiveHandler = receiveHandler;
            _db = db;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var copies = _db.BookCopies
                .Where(c => c.Status == BookCopyStatus.InTransfer)
                .ToList();

            return View("Index", copies);
        }

        [HttpGet]
        [ActionName("Request")]
        public IActionResult RequestGet()
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
        [ActionName("Request")]
        public IActionResult RequestPost(TransferRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Books = _db.Books
                    .Select(b => new SelectListItem { Value = b.Id.ToString(), Text = b.Title })
                    .ToList();

                model.Sites = _db.Sites
                    .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
                    .ToList();

                return View("Request", model);
            }

            // Correction : stocker le dernier résultat pour l’envoyer à la vue
            RequestTransferResult lastResult = null;

            for (var i = 0; i < model.Quantity; i++)
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

                    return View("Request", model);
                }

                lastResult = result;
            }

            return View("RequestSuccess", lastResult);
        }

        [HttpPost]
        public IActionResult Receive(Guid bookCopyId, Guid targetSiteId)
        {
            var command = new ReceiveTransferCommand(bookCopyId, targetSiteId);
            var result = _receiveHandler.Handle(command);

            if (!result.Success)
                return BadRequest(result.ErrorCode);

            return View("ReceiveSuccess");
        }


        [HttpGet]
        public IActionResult ConfirmReceive(Guid bookCopyId, Guid targetSiteId)
        {
            var command = new ReceiveTransferCommand(bookCopyId, targetSiteId);
            var result = _receiveHandler.Handle(command);

            if (!result.Success)
                return BadRequest(result.ErrorCode);

            return View("ReceiveSuccess");
        }

    }
}
