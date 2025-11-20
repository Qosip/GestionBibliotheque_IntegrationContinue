using System;
using Library.Domain.Enums;

namespace Library.Domain.Entities;

public class BookCopy
{
    public Guid Id { get; private set; }
    public Guid BookId { get; private set; }
    public Guid SiteId { get; private set; }
    public BookCopyStatus Status { get; private set; }

    
    // Ctor métier
    public BookCopy(Guid bookId, Guid siteId)
    {
        if (bookId == Guid.Empty)
            throw new ArgumentException("BookId cannot be empty.", nameof(bookId));

        if (siteId == Guid.Empty)
            throw new ArgumentException("SiteId cannot be empty.", nameof(siteId));

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
        if (newSiteId == Guid.Empty)
            throw new ArgumentException("SiteId cannot be empty.", nameof(newSiteId));

        SiteId = newSiteId;
    }

    public void MarkAsInTransfer()
    {
        if (Status != BookCopyStatus.Available)
            throw new InvalidOperationException("Copy must be available to be put in transfer.");

        Status = BookCopyStatus.InTransfer;
    }

    public void MarkAsArrived(Guid newSiteId)
    {
        if (Status != BookCopyStatus.InTransfer)
            throw new InvalidOperationException("Copy must be in transfer to arrive at a site.");

        if (newSiteId == Guid.Empty)
            throw new ArgumentException("SiteId cannot be empty.", nameof(newSiteId));

        SiteId = newSiteId;
        Status = BookCopyStatus.Available;
    }
}
