using System;

namespace Library.Application.Commands;

public sealed class RegisterUserCommand
{
    public string Name { get; set; } = string.Empty;

    // Ctor sans paramètre pour MVC / Razor
    public RegisterUserCommand()
    {
    }

    // Ctor pratique pour les tests / code
    public RegisterUserCommand(string name)
    {
        Name = name;
    }
}
