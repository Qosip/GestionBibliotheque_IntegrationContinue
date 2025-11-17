using System;
using Library.Application.Commands;
using Library.Application.Repositories;
using Library.Application.Results;
using Library.Domain;

namespace Library.Application.Handlers;

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

        // Nouvelle validation : on refuse si source et cible identiques
        if (command.SourceSiteId == command.TargetSiteId)
        {
            return RequestTransferResult.Fail("SOURCE_AND_TARGET_MUST_DIFFER");
        }

        // Cherche une copie disponible sur le site source
        var copy = _bookCopyRepository.FindAvailableCopy(command.BookId, command.SourceSiteId);
        if (copy is null)
        {
            return RequestTransferResult.Fail("NO_COPY_AVAILABLE_AT_SOURCE_SITE");
        }

        copy.MarkAsInTransfer();

        return RequestTransferResult.Ok(copy.Id);
    }
}
