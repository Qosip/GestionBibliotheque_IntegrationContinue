namespace Library.Domain;

public class BorrowResult
{
    public bool Success { get; }
    public string? ErrorCode { get; }

    private BorrowResult(bool success, string? errorCode)
    {
        Success = success;
        ErrorCode = errorCode;
    }

    public static BorrowResult Ok() => new BorrowResult(true, null);

    public static BorrowResult Fail(string errorCode) => new BorrowResult(false, errorCode);
}
