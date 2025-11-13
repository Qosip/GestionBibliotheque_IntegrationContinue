using System;

namespace Library.Application;

public sealed class RegisterUserCommand
{
    public string Name { get; }

    public RegisterUserCommand(string name)
    {
        Name = name;
    }
}
