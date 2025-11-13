using System;
using System.Collections.Generic;
using Library.Application;
using Library.Domain;
using Xunit;

namespace Library.Tests;

public class RegisterUserHandlerTests
{
    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly Dictionary<Guid, UserAccount> _users = new();

        public UserAccount? GetById(Guid id) =>
            _users.TryGetValue(id, out var user) ? user : null;

        public void Add(UserAccount user) => _users[user.Id] = user;
    }

    [Fact]
    public void Handle_creates_user_and_returns_id()
    {
        // Arrange
        var repo = new FakeUserRepository();
        var handler = new RegisterUserHandler(repo);

        var command = new RegisterUserCommand("Alice");

        // Act
        var result = handler.Handle(command);

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.ErrorCode);
        Assert.NotEqual(Guid.Empty, result.UserId);

        var user = repo.GetById(result.UserId);
        Assert.NotNull(user);
        Assert.Equal(0, user!.ActiveLoansCount);
        Assert.Equal(0m, user.AmountDue);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Handle_returns_error_when_name_invalid(string? name)
    {
        // Arrange
        var repo = new FakeUserRepository();
        var handler = new RegisterUserHandler(repo);
        var command = new RegisterUserCommand(name!);

        // Act
        var result = handler.Handle(command);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("INVALID_USER_NAME", result.ErrorCode);
    }
}
