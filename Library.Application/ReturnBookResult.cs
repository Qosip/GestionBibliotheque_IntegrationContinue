using System;

namespace Library.Application;

public sealed class ReturnBookResult
{
    public bool Success { get; }
    public string? ErrorCode { get; }

    private ReturnBookResult(bool success, string? errorCode)
    {
        Success = success;
        ErrorCode = errorCode;
    }

    public static ReturnBookResult Ok() =>
        new ReturnBookResult(true, null);

    public static ReturnBookResult Fail(string errorCode) =>
        new ReturnBookResult(false, errorCode);
}
