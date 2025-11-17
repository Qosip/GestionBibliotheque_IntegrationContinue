using System;

namespace Library.Domain.Entities;

public class Site
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Address { get; private set; }

    private Site()
    {
    }
    public Site(Guid id, string name, string? address = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Site name cannot be empty.", nameof(name));

        Id = id;
        Name = name.Trim();
        Address = address;
    }
}
