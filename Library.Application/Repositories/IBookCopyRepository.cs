using Library.Domain.Entities;

namespace Library.Application.Repositories;

public interface IBookCopyRepository
{
    BookCopy? GetById(Guid id);
    BookCopy? FindAvailableCopy(Guid bookId, Guid siteId);
    IEnumerable<BookCopy> GetByBook(Guid bookId);

    void Add(BookCopy copy);
    void Update(BookCopy copy);
}