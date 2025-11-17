using System;

namespace Library.Application.Commands;

public sealed class RequestTransferCommand
{
    public Guid BookId { get; set; }
    public Guid SourceSiteId { get; set; }
    public Guid TargetSiteId { get; set; }

    public RequestTransferCommand()
    {
    }

    public RequestTransferCommand(Guid bookId, Guid sourceSiteId, Guid targetSiteId)
    {
        BookId = bookId;
        SourceSiteId = sourceSiteId;
        TargetSiteId = targetSiteId;
    }
}
