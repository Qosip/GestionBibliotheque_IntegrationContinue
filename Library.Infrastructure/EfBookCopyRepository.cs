using System;
using System.Linq;
using Library.Application;
using Library.Domain;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure;

public class EfBookCopyRepository : IBookCopyRepository
{
    private readonly LibraryDbContext _context;

    public EfBookCopyRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public BookCopy? FindAvailableCopy(Guid bookId, Guid siteId)
    {
        return _context.BookCopies
            .AsNoTracking()
            .FirstOrDefault(c =>
                c.BookId == bookId &&
                c.SiteId == siteId &&
                c.Status == BookCopyStatus.Available);
    }

    public BookCopy? GetById(Guid id) =>
        _context.BookCopies.SingleOrDefault(c => c.Id == id);

    public void Add(BookCopy copy)
    {
        _context.BookCopies.Add(copy);
        _context.SaveChanges();
    }
}
