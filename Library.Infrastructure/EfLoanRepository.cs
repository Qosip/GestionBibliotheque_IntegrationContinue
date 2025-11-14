using System;
using System.Linq;
using Library.Application;
using Library.Domain;

namespace Library.Infrastructure;

public class EfLoanRepository : ILoanRepository
{
    private readonly LibraryDbContext _context;

    public EfLoanRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public void Add(Loan loan)
    {
        _context.Loans.Add(loan);
        _context.SaveChanges();
    }

    public Loan? GetById(Guid id) =>
        _context.Loans.SingleOrDefault(l => l.Id == id);
}
