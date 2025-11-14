using System;
using System.Linq;
using Library.Application;
using Library.Domain;

namespace Library.Infrastructure;

public class EfSiteRepository : ISiteRepository
{
    private readonly LibraryDbContext _context;

    public EfSiteRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public Site? GetById(Guid id) =>
        _context.Sites.SingleOrDefault(s => s.Id == id);

    public void Add(Site site)
    {
        _context.Sites.Add(site);
        _context.SaveChanges();
    }
}
