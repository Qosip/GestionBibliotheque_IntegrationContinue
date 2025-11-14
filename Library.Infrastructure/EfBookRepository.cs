using System;
using System.Linq;
using Library.Application;
using Library.Domain;

namespace Library.Infrastructure;

public class EfBookRepository : IBookRepository
{
    private readonly LibraryDbContext _context;

    public EfBookRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public Book? GetById(Guid id) =>
        _context.Books.SingleOrDefault(b => b.Id == id);

    public void Add(Book book)
    {
        _context.Books.Add(book);
        _context.SaveChanges();
    }
}
