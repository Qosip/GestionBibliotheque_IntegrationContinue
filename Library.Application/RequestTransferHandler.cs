using System;
using Library.Domain;

namespace Library.Application;

public sealed class RequestTransferHandler
{
    private readonly IBookCopyRepository _bookCopyRepository;

    public RequestTransferHandler(IBookCopyRepository bookCopyRepository)
    {
        _bookCopyRepository = bookCopyRepository ?? throw new ArgumentNullException(nameof(bookCopyRepository));
    }

    public RequestTransferResult Handle(RequestTransferCommand command)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));

        // Cherche une copie disponible sur le site source
        var copy = _bookCopyRepository.FindAvailableCopy(command.BookId, command.SourceSiteId);
        if (copy is null)
        {
            return RequestTransferResult.Fail("NO_COPY_AVAILABLE_AT_SOURCE_SITE");
        }

        // On la met en transfert
        copy.MarkAsInTransfer();

        // (Dans une vraie infra, on persisterait l'update via le repo)
        return RequestTransferResult.Ok(copy.Id);
    }
}
