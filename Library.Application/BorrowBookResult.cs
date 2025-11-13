using System;

namespace Library.Application;

public sealed class BorrowBookResult
{
    public bool Success { get; }
    public string? ErrorCode { get; }
    public Guid LoanId { get; }

    private BorrowBookResult(bool success, string? errorCode, Guid loanId)
    {
        Success = success;
        ErrorCode = errorCode;
        LoanId = loanId;
    }

    public static BorrowBookResult Ok(Guid loanId) =>
        new BorrowBookResult(true, null, loanId);

    public static BorrowBookResult Fail(string errorCode) =>
        new BorrowBookResult(false, errorCode, Guid.Empty);
}
