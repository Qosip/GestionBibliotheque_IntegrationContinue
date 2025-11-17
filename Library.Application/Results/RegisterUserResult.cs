using System;

namespace Library.Application.Results;

public sealed class RegisterUserResult
{
    public bool Success { get; }
    public string? ErrorCode { get; }
    public Guid UserId { get; }

    private RegisterUserResult(bool success, string? errorCode, Guid userId)
    {
        Success = success;
        ErrorCode = errorCode;
        UserId = userId;
    }

    public static RegisterUserResult Ok(Guid userId) =>
        new RegisterUserResult(true, null, userId);

    public static RegisterUserResult Fail(string errorCode) =>
        new RegisterUserResult(false, errorCode, Guid.Empty);
}
