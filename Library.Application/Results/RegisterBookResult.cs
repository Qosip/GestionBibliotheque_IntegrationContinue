using System;

namespace Library.Application.Results;

public sealed class RegisterBookResult
{
    public bool Success { get; }
    public string? ErrorCode { get; }
    public Guid BookId { get; }

    private RegisterBookResult(bool success, string? errorCode, Guid bookId)
    {
        Success = success;
        ErrorCode = errorCode;
        BookId = bookId;
    }

    public static RegisterBookResult Ok(Guid bookId) =>
        new RegisterBookResult(true, null, bookId);

    public static RegisterBookResult Fail(string errorCode) =>
        new RegisterBookResult(false, errorCode, Guid.Empty);
}
