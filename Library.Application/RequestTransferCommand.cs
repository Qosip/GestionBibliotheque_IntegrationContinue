using System;

namespace Library.Application;

public sealed class RequestTransferCommand
{
    public Guid BookId { get; }
    public Guid SourceSiteId { get; }
    public Guid TargetSiteId { get; }

    public RequestTransferCommand(Guid bookId, Guid sourceSiteId, Guid targetSiteId)
    {
        if (sourceSiteId == targetSiteId)
            throw new ArgumentException("Source and target sites must be different.", nameof(targetSiteId));

        BookId = bookId;
        SourceSiteId = sourceSiteId;
        TargetSiteId = targetSiteId;
    }
}
