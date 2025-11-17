using System;

namespace Library.Application.Results;

public sealed class AddBookCopyResult
{
    public bool Success { get; }
    public string? ErrorCode { get; }
    public Guid BookCopyId { get; }

    private AddBookCopyResult(bool success, string? errorCode, Guid bookCopyId)
    {
        Success = success;
        ErrorCode = errorCode;
        BookCopyId = bookCopyId;
    }

    public static AddBookCopyResult Ok(Guid bookCopyId) =>
        new AddBookCopyResult(true, null, bookCopyId);

    public static AddBookCopyResult Fail(string errorCode) =>
        new AddBookCopyResult(false, errorCode, Guid.Empty);
}
