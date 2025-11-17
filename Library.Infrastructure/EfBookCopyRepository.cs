using System;
using System.Collections.Generic;
using System.Linq;
using Library.Application.Repositories;
using Library.Domain.Entities;
using Library.Domain.Enums;

namespace Library.Infrastructure;

public class EfBookCopyRepository : IBookCopyRepository
{
    private readonly LibraryDbContext _context;

    public EfBookCopyRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public BookCopy? GetById(Guid id)
        => _context.BookCopies.SingleOrDefault(c => c.Id == id);

    public BookCopy? FindAvailableCopy(Guid bookId, Guid siteId)
        => _context.BookCopies.FirstOrDefault(c =>
            c.BookId == bookId &&
            c.SiteId == siteId &&
            c.Status == BookCopyStatus.Available);

    public IEnumerable<BookCopy> GetByBook(Guid bookId)
        => _context.BookCopies
            .Where(c => c.BookId == bookId)
            .ToList();

    public void Add(BookCopy copy)
    {
        _context.BookCopies.Add(copy);
        _context.SaveChanges();
    }

    public void Update(BookCopy copy)
    {
        _context.BookCopies.Update(copy);
        _context.SaveChanges();
    }
}