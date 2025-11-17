using Library.Domain.Entities;

namespace Library.Application.Repositories;

public interface ILoanRepository
{
    Loan? GetById(Guid id);

    IEnumerable<Loan> GetActiveLoansForUser(Guid userId);
    Loan? GetActiveLoanForUserAndCopy(Guid userId, Guid bookCopyId);

    void Add(Loan loan);
    void Update(Loan loan);
}