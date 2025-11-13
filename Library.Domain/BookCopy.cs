using System;

namespace Library.Domain;

public class BookCopy
{
    public Guid Id { get; }
    public Guid BookId { get; }
    public Guid SiteId { get; private set; }
    public BookCopyStatus Status { get; private set; }

    public BookCopy(Guid bookId, Guid siteId)
    {
        Id = Guid.NewGuid();
        BookId = bookId;
        SiteId = siteId;
        Status = BookCopyStatus.Available;
    }

    public void MarkAsBorrowed()
    {
        if (Status != BookCopyStatus.Available)
            throw new InvalidOperationException("Copy must be available to be borrowed.");

        Status = BookCopyStatus.Borrowed;
    }

    public void MarkAsReturned()
    {
        if (Status != BookCopyStatus.Borrowed)
            throw new InvalidOperationException("Copy must be borrowed to be returned.");

        Status = BookCopyStatus.Available;
    }

    public void MoveToSite(Guid newSiteId)
    {
        SiteId = newSiteId;
    }
}
