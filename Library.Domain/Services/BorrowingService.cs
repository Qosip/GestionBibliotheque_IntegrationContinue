using System;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Library.Domain.Results;

namespace Library.Domain.Services;

public class BorrowingService
{
    private const int MaxActiveLoans = 5;

    public BorrowResult TryBorrow(UserAccount user, BookCopy copy, DateTime borrowedAt, DateTime dueDate)
    {
        if (user is null) throw new ArgumentNullException(nameof(user));
        if (copy is null) throw new ArgumentNullException(nameof(copy));

        if (user.ActiveLoansCount >= MaxActiveLoans)
        {
            return BorrowResult.Fail("BORROW_LIMIT_REACHED");
        }

        if (copy.Status != BookCopyStatus.Available)
        {
            return BorrowResult.Fail("COPY_NOT_AVAILABLE");
        }

        if (dueDate < borrowedAt)
        {
            throw new ArgumentException("Due date cannot be before borrowed date.", nameof(dueDate));
        }

        // Création du Loan métier
        var loan = new Loan(user.Id, copy.Id, borrowedAt, dueDate);

        // Mise à jour des états
        copy.MarkAsBorrowed();
        user.IncrementLoans();

        return BorrowResult.Ok(loan);
    }
}
