using System;
using Library.Application.Commands;
using Library.Application.Repositories;
using Library.Application.Results;
using Library.Domain.Enums;

namespace Library.Application.Handlers
{
    public sealed class ReceiveTransferHandler
    {
        private readonly IBookCopyRepository _repo;

        public ReceiveTransferHandler(IBookCopyRepository repo)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public ReceiveTransferResult Handle(ReceiveTransferCommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            var copy = _repo.GetById(command.BookCopyId);
            if (copy == null)
                return ReceiveTransferResult.Fail("COPY_NOT_FOUND");

            if (copy.Status != BookCopyStatus.InTransfer)
                return ReceiveTransferResult.Fail("COPY_NOT_IN_TRANSFER");

            try
            {
                copy.MarkAsArrived(command.TargetSiteId);
                _repo.Update(copy);
            }
            catch (Exception ex)
            {
                return ReceiveTransferResult.Fail(ex.GetType().Name);
            }

            return ReceiveTransferResult.Ok();
        }
    }
}
