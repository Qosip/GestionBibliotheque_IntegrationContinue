using System;
using Library.Domain;

namespace Library.Application;

public sealed class CreateSiteHandler
{
    private readonly ISiteRepository _siteRepository;

    public CreateSiteHandler(ISiteRepository siteRepository)
    {
        _siteRepository = siteRepository ?? throw new ArgumentNullException(nameof(siteRepository));
    }

    public CreateSiteResult Handle(CreateSiteCommand command)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return CreateSiteResult.Fail("INVALID_SITE_NAME");
        }

        var site = new Site(Guid.NewGuid(), command.Name, command.Address);
        _siteRepository.Add(site);

        return CreateSiteResult.Ok(site.Id);
    }
}
