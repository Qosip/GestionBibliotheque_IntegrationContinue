using System;

namespace Library.Application.Commands;

public sealed class ReturnBookCommand
{
    public Guid LoanId { get; set; }

    public ReturnBookCommand()
    {
    }

    public ReturnBookCommand(Guid loanId)
    {
        LoanId = loanId;
    }
}
