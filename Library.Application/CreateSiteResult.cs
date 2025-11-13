using System;

namespace Library.Application;

public sealed class CreateSiteResult
{
    public bool Success { get; }
    public string? ErrorCode { get; }
    public Guid SiteId { get; }

    private CreateSiteResult(bool success, string? errorCode, Guid siteId)
    {
        Success = success;
        ErrorCode = errorCode;
        SiteId = siteId;
    }

    public static CreateSiteResult Ok(Guid siteId) =>
        new CreateSiteResult(true, null, siteId);

    public static CreateSiteResult Fail(string errorCode) =>
        new CreateSiteResult(false, errorCode, Guid.Empty);
}
