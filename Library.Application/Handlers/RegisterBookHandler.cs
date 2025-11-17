using System;
using Library.Application.Commands;
using Library.Application.Repositories;
using Library.Application.Results;
using Library.Domain;
using Library.Domain.Entities;

namespace Library.Application.Handlers;

public sealed class RegisterBookHandler
{
    private readonly IBookRepository _bookRepository;

    public RegisterBookHandler(IBookRepository bookRepository)
    {
        _bookRepository = bookRepository ?? throw new ArgumentNullException(nameof(bookRepository));
    }

    public RegisterBookResult Handle(RegisterBookCommand command)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));

        if (string.IsNullOrWhiteSpace(command.Isbn) ||
            string.IsNullOrWhiteSpace(command.Title) ||
            string.IsNullOrWhiteSpace(command.Author))
        {
            return RegisterBookResult.Fail("INVALID_BOOK_DATA");
        }

        var book = new Book(Guid.NewGuid(), command.Isbn, command.Title, command.Author);
        _bookRepository.Add(book);

        return RegisterBookResult.Ok(book.Id);
    }
}
