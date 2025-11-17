using System;

namespace Library.Application.Commands;

public sealed class BorrowBookCommand
{
    public Guid UserId { get; set; }
    public Guid BookId { get; set; }
    public Guid SiteId { get; set; }

    public BorrowBookCommand()
    {
    }

    public BorrowBookCommand(Guid userId, Guid bookId, Guid siteId)
    {
        UserId = userId;
        BookId = bookId;
        SiteId = siteId;
    }
}
