using System;
using System.Collections.Generic;
using System.Linq;
using Library.Application.Repositories;
using Library.Domain.Entities;

namespace Library.Infrastructure;

public class EfLoanRepository : ILoanRepository
{
    private readonly LibraryDbContext _context;

    public EfLoanRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public Loan? GetById(Guid id)
        => _context.Loans.SingleOrDefault(l => l.Id == id);

    public IEnumerable<Loan> GetActiveLoansForUser(Guid userId)
        => _context.Loans
            .Where(l => l.UserAccountId == userId && l.ReturnedAt == null)
            .ToList();

    public Loan? GetActiveLoanForUserAndCopy(Guid userId, Guid bookCopyId)
        => _context.Loans
            .SingleOrDefault(l =>
                l.UserAccountId == userId &&
                l.BookCopyId == bookCopyId &&
                l.ReturnedAt == null);

    public void Add(Loan loan)
    {
        _context.Loans.Add(loan);
        _context.SaveChanges();
    }

    public void Update(Loan loan)
    {
        _context.Loans.Update(loan);
        _context.SaveChanges();
    }
}