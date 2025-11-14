using System;
using System.Linq;
using Library.Application;
using Library.Domain;

namespace Library.Infrastructure;

public class EfUserRepository : IUserRepository
{
    private readonly LibraryDbContext _context;

    public EfUserRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public UserAccount? GetById(Guid id) =>
        _context.UserAccounts.SingleOrDefault(u => u.Id == id);

    public void Add(UserAccount user)
    {
        _context.UserAccounts.Add(user);
        _context.SaveChanges();
    }
}
