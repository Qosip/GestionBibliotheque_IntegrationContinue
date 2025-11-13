using System;
using Library.Domain;

namespace Library.Application;

public interface IBookCopyRepository
{
    BookCopy? FindAvailableCopy(Guid bookId, Guid siteId);
}
