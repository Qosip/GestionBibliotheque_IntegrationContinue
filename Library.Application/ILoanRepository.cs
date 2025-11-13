using System;
using Library.Domain;

namespace Library.Application;

public interface ILoanRepository
{
    void Add(Loan loan);
    Loan? GetById(Guid id);
}
