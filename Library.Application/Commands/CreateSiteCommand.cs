using System;

namespace Library.Application.Commands;

public sealed class CreateSiteCommand
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }

    public CreateSiteCommand()
    {
    }

    public CreateSiteCommand(string name, string? address)
    {
        Name = name;
        Address = address;
    }
}
