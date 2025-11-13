using System;

namespace Library.Application;

public sealed class ReturnBookCommand
{
    public Guid LoanId { get; }

    public ReturnBookCommand(Guid loanId)
    {
        LoanId = loanId;
    }
}
