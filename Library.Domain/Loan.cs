using System;

namespace Library.Domain;

public class Loan
{
    public Guid Id { get; }
    public Guid UserAccountId { get; }
    public Guid BookCopyId { get; }
    public DateTime BorrowedAt { get; }
    public DateTime DueDate { get; }
    public DateTime? ReturnedAt { get; private set; }

    // Constructeur existant (utilisé par LoanOverdueTests)
    public Loan(DateTime borrowedAt, DateTime dueDate)
    {
        if (dueDate < borrowedAt)
            throw new ArgumentException("Due date cannot be before borrowed date.", nameof(dueDate));

        Id = Guid.NewGuid();
        BorrowedAt = borrowedAt;
        DueDate = dueDate;
        UserAccountId = Guid.Empty;
        BookCopyId = Guid.Empty;
    }

    // Nouveau constructeur complet (utilisé par BorrowingService)
    public Loan(Guid userAccountId, Guid bookCopyId, DateTime borrowedAt, DateTime dueDate)
        : this(borrowedAt, dueDate)
    {
        UserAccountId = userAccountId;
        BookCopyId = bookCopyId;
    }

    public bool IsOverdue(DateTime now)
    {
        if (now < BorrowedAt)
            throw new ArgumentOutOfRangeException(nameof(now), "Now cannot be before borrowed date.");

        var referenceDate = ReturnedAt ?? now;
        return referenceDate.Date > DueDate.Date;
    }

    public int GetOverdueDays(DateTime now)
    {
        if (now < BorrowedAt)
            throw new ArgumentOutOfRangeException(nameof(now), "Now cannot be before borrowed date.");

        if (!IsOverdue(now))
            return 0;

        var referenceDate = ReturnedAt ?? now;
        return (referenceDate.Date - DueDate.Date).Days;
    }

    public void MarkAsReturned(DateTime returnedAt)
    {
        if (returnedAt < BorrowedAt)
            throw new ArgumentOutOfRangeException(nameof(returnedAt), "Return date cannot be before borrowed date.");

        ReturnedAt = returnedAt;
    }
}
