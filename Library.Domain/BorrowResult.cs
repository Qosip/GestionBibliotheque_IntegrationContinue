namespace Library.Domain;

public class BorrowResult
{
    public bool Success { get; }
    public string? ErrorCode { get; }
    public Loan? Loan { get; }

    private BorrowResult(bool success, string? errorCode, Loan? loan)
    {
        Success = success;
        ErrorCode = errorCode;
        Loan = loan;
    }

    public static BorrowResult Ok(Loan loan) =>
        new BorrowResult(true, null, loan);

    public static BorrowResult Fail(string errorCode) =>
        new BorrowResult(false, errorCode, null);
}
