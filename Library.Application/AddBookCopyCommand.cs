using System;

namespace Library.Application;

public sealed class AddBookCopyCommand
{
    public Guid BookId { get; set; }
    public Guid SiteId { get; set; }

    public AddBookCopyCommand()
    {
    }

    public AddBookCopyCommand(Guid bookId, Guid siteId)
    {
        BookId = bookId;
        SiteId = siteId;
    }
}
