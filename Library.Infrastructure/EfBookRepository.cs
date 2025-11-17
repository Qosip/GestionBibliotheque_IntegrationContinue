using System;
using System.Linq;
using System.Collections.Generic;
using Library.Domain.Entities;
using Library.Application.Repositories;

namespace Library.Infrastructure;

public class EfBookRepository : IBookRepository
{
    private readonly LibraryDbContext _context;

    public EfBookRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public Book? GetById(Guid id)
        => _context.Books.SingleOrDefault(b => b.Id == id);

    public IEnumerable<Book> GetAll()
        => _context.Books.ToList();

    public void Add(Book book)
    {
        _context.Books.Add(book);
        _context.SaveChanges();
    }

    public void Update(Book book)
    {
        _context.Books.Update(book);
        _context.SaveChanges(); 
    }
}
