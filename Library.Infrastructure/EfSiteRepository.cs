using System;
using System.Collections.Generic;
using System.Linq;
using Library.Application.Repositories;
using Library.Domain.Entities;

namespace Library.Infrastructure;

public class EfSiteRepository : ISiteRepository
{
    private readonly LibraryDbContext _context;

    public EfSiteRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public Site? GetById(Guid id)
        => _context.Sites.SingleOrDefault(x => x.Id == id);

    public IEnumerable<Site> GetAll()
        => _context.Sites.ToList();

    public void Add(Site site)
    {
        _context.Sites.Add(site);
        _context.SaveChanges();
    }

    public void Update(Site site)
    {
        _context.Sites.Update(site);
        _context.SaveChanges();
    }
}
