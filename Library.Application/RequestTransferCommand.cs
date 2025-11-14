using System;

namespace Library.Application;

public sealed class RequestTransferCommand
{
    public Guid BookId { get; set; }
    public Guid SourceSiteId { get; set; }
    public Guid TargetSiteId { get; set; }

    // Constructeur sans paramètre pour MVC / Razor (GET, binding)
    public RequestTransferCommand()
    {
    }

    // Optionnel : constructeur pratique pour les tests / code
    public RequestTransferCommand(Guid bookId, Guid sourceSiteId, Guid targetSiteId)
    {
        BookId = bookId;
        SourceSiteId = sourceSiteId;
        TargetSiteId = targetSiteId;
    }
}
