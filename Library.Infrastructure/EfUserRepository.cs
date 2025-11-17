using System;
using System.Collections.Generic;
using System.Linq;
using Library.Application.Repositories;
using Library.Domain.Entities;
using SQLitePCL;

namespace Library.Infrastructure;

public class EfUserRepository : IUserRepository
{
    private readonly LibraryDbContext _context;

    public EfUserRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public UserAccount? GetById(Guid id)
        => _context.UserAccounts.SingleOrDefault(u => u.Id == id);
  

    public IEnumerable<UserAccount> GetAll()
        => _context.UserAccounts.ToList();

    public void Add(UserAccount user)
    {
        _context.UserAccounts.Add(user);
        _context.SaveChanges();
    }

    public void Update(UserAccount user)
    {
        _context.UserAccounts.Update(user);
        _context.SaveChanges();
    }
}