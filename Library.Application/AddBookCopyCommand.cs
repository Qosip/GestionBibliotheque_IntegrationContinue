using System;

namespace Library.Application;

public sealed class AddBookCopyCommand
{
    public Guid BookId { get; }
    public Guid SiteId { get; }

    public AddBookCopyCommand(Guid bookId, Guid siteId)
    {
        BookId = bookId;
        SiteId = siteId;
    }
}
