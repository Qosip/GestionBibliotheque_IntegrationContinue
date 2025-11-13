using System;

namespace Library.Application;

public sealed class RequestTransferResult
{
    public bool Success { get; }
    public string? ErrorCode { get; }
    public Guid BookCopyId { get; }

    private RequestTransferResult(bool success, string? errorCode, Guid bookCopyId)
    {
        Success = success;
        ErrorCode = errorCode;
        BookCopyId = bookCopyId;
    }

    public static RequestTransferResult Ok(Guid bookCopyId) =>
        new RequestTransferResult(true, null, bookCopyId);

    public static RequestTransferResult Fail(string errorCode) =>
        new RequestTransferResult(false, errorCode, Guid.Empty);
}
