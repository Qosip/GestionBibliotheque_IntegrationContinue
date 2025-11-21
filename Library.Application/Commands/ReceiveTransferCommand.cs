using System;

namespace Library.Application.Commands
{
    public sealed class ReceiveTransferCommand
    {
        public Guid BookCopyId { get; }
        public Guid TargetSiteId { get; }

        public ReceiveTransferCommand(Guid bookCopyId, Guid targetSiteId)
        {
            BookCopyId = bookCopyId;
            TargetSiteId = targetSiteId;
        }
    }
}
