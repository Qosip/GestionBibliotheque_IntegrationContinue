using System;
using Library.Application.Commands;
using Library.Application.Repositories;
using Library.Application.Results;
using Library.Domain;
using Library.Domain.Entities;

namespace Library.Application.Handlers;

public sealed class RegisterUserHandler
{
    private readonly IUserRepository _userRepository;

    public RegisterUserHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public RegisterUserResult Handle(RegisterUserCommand command)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return RegisterUserResult.Fail("INVALID_USER_NAME");
        }

        var user = new UserAccount(
            id: Guid.NewGuid(),
            command.Name,
            activeLoansCount: 0,
            amountDue: 0m);

        _userRepository.Add(user);

        return RegisterUserResult.Ok(user.Id);
    }
}
