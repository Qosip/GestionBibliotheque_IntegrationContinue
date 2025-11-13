using System;
using Library.Domain;

namespace Library.Application;

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
            activeLoansCount: 0,
            amountDue: 0m);

        _userRepository.Add(user);

        return RegisterUserResult.Ok(user.Id);
    }
}
