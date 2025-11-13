using System;
using Library.Domain;

namespace Library.Application;

public interface IBookRepository
{
    Book? GetById(Guid id);
    void Add(Book book);
}
