using System;

namespace Library.Domain;

public class Site
{
    public Guid Id { get; }
    public string Name { get; }
    public string? Address { get; }

    public Site(Guid id, string name, string? address = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Site name cannot be empty.", nameof(name));

        Id = id;
        Name = name.Trim();
        Address = address;
    }
}
