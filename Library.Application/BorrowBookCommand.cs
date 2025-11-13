using System;

namespace Library.Application;

public sealed class BorrowBookCommand
{
    public Guid UserId { get; }
    public Guid BookId { get; }
    public Guid SiteId { get; }

    public BorrowBookCommand(Guid userId, Guid bookId, Guid siteId)
    {
        UserId = userId;
        BookId = bookId;
        SiteId = siteId;
    }
}
