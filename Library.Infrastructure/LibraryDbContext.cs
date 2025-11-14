using System.Collections.Generic;
using System.Reflection.Emit;
using Library.Domain;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure;

public class LibraryDbContext : DbContext
{
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<BookCopy> BookCopies => Set<BookCopy>();
    public DbSet<Loan> Loans => Set<Loan>();

    public LibraryDbContext(DbContextOptions<LibraryDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Config minimale, EF peut gérer les types simples avec les constructeurs existants.
        base.OnModelCreating(modelBuilder);
    }
}
