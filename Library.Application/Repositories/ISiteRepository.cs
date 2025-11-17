using System;
using Library.Domain.Entities;

namespace Library.Application.Repositories;


public interface ISiteRepository
{
    Site? GetById(Guid id);
    IEnumerable<Site> GetAll();
    void Add(Site site);
    void Update(Site site);
}