using System;

namespace Library.Application;

public sealed class CreateSiteCommand
{
    public string Name { get; }
    public string? Address { get; }

    public CreateSiteCommand(string name, string? address)
    {
        Name = name;
        Address = address;
    }
}
