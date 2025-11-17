using System;
using Library.Application.Commands;
using Library.Application.Repositories;
using Library.Application.Results;
using Library.Domain;
using Library.Domain.Entities;

namespace Library.Application.Handlers;

public sealed class AddBookCopyHandler
{
    private readonly IBookRepository _bookRepository;
    private readonly ISiteRepository _siteRepository;
    private readonly IBookCopyRepository _bookCopyRepository;

    public AddBookCopyHandler(
        IBookRepository bookRepository,
        ISiteRepository siteRepository,
        IBookCopyRepository bookCopyRepository)
    {
        _bookRepository = bookRepository ?? throw new ArgumentNullException(nameof(bookRepository));
        _siteRepository = siteRepository ?? throw new ArgumentNullException(nameof(siteRepository));
        _bookCopyRepository = bookCopyRepository ?? throw new ArgumentNullException(nameof(bookCopyRepository));
    }

    public AddBookCopyResult Handle(AddBookCopyCommand command)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));

        var book = _bookRepository.GetById(command.BookId);
        if (book is null)
            return AddBookCopyResult.Fail("BOOK_NOT_FOUND");

        var site = _siteRepository.GetById(command.SiteId);
        if (site is null)
            return AddBookCopyResult.Fail("SITE_NOT_FOUND");

        var copy = new BookCopy(command.BookId, command.SiteId);
        _bookCopyRepository.Add(copy);

        return AddBookCopyResult.Ok(copy.Id);
    }
}
