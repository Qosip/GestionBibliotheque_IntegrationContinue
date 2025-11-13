using System;
using Library.Domain;

namespace Library.Application;

public interface ISiteRepository
{
    Site? GetById(Guid id);
    void Add(Site site);
}
