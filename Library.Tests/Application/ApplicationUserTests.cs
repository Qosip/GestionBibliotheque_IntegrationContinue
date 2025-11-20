using System;
using System.Collections.Generic;
using System.Linq;
using Library.Application.Commands;
using Library.Application.Handlers;
using Library.Application.Repositories;
using Library.Application.Results;
using Library.Domain.Entities;
using Xunit;

namespace Library.Tests.Application
{
    public sealed class ApplicationUserTests
    {
        #region Fakes

        private sealed class FakeUserRepository : IUserRepository
        {
            private readonly List<UserAccount> _users = new();

            public IReadOnlyList<UserAccount> Users => _users.AsReadOnly();

            public UserAccount? GetById(Guid id) =>
                _users.SingleOrDefault(u => u.Id == id);

            public IEnumerable<UserAccount> GetAll() =>
                _users.AsReadOnly();

            public void Add(UserAccount user)
            {
                if (user == null) throw new ArgumentNullException(nameof(user));
                _users.Add(user);
            }

            public void Update(UserAccount user)
            {
                if (user == null) throw new ArgumentNullException(nameof(user));

                var index = _users.FindIndex(u => u.Id == user.Id);
                if (index >= 0)
                {
                    _users[index] = user;
                }
            }
        }

        #endregion

        #region RegisterUserResult

        [Fact]
        public void RegisterUserResult_Ok_Should_SetSuccess_UserId_AndNullErrorCode()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var result = RegisterUserResult.Ok(userId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(userId, result.UserId);
            Assert.Null(result.ErrorCode);
        }

        [Fact]
        public void RegisterUserResult_Fail_Should_SetFailure_EmptyUserId_AndErrorCode()
        {
            // Arrange
            const string errorCode = "ANY_ERROR_CODE";

            // Act
            var result = RegisterUserResult.Fail(errorCode);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(Guid.Empty, result.UserId);
            Assert.Equal(errorCode, result.ErrorCode);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void RegisterUserResult_Fail_Should_Tolerate_NullOrWhitespace_ErrorCode(string? errorCode)
        {
            // Arrange
            // Act
            var result = RegisterUserResult.Fail(errorCode!);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(Guid.Empty, result.UserId);
            Assert.Equal(errorCode, result.ErrorCode);
        }

        #endregion

        #region RegisterUserCommand

        [Fact]
        public void RegisterUserCommand_ParameterlessCtor_Should_Default_Name_To_Empty()
        {
            // Arrange
            // Act
            var command = new RegisterUserCommand();

            // Assert
            Assert.Equal(string.Empty, command.Name);
        }

        [Fact]
        public void RegisterUserCommand_ParameterizedCtor_Should_Set_Name()
        {
            // Arrange
            const string name = "Alice";

            // Act
            var command = new RegisterUserCommand(name);

            // Assert
            Assert.Equal(name, command.Name);
        }

        [Fact]
        public void RegisterUserCommand_Name_Should_Be_Mutable()
        {
            // Arrange
            var command = new RegisterUserCommand();

            // Act
            command.Name = "Bob";

            // Assert
            Assert.Equal("Bob", command.Name);
        }

        #endregion

        #region RegisterUserHandler – construction

        [Fact]
        public void RegisterUserHandler_Ctor_Should_Throw_When_Repository_Is_Null()
        {
            // Arrange
            IUserRepository? repo = null;

            // Act
            var exception = Assert.Throws<ArgumentNullException>(() => new RegisterUserHandler(repo!));

            // Assert
            Assert.Equal("userRepository", exception.ParamName);
        }

        #endregion

        #region RegisterUserHandler – validation

        [Fact]
        public void RegisterUserHandler_Handle_Should_Throw_When_Command_Is_Null()
        {
            // Arrange
            var repo = new FakeUserRepository();
            var handler = new RegisterUserHandler(repo);

            // Act
            var exception = Assert.Throws<ArgumentNullException>(() => handler.Handle(null!));

            // Assert
            Assert.Equal("command", exception.ParamName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void RegisterUserHandler_Should_ReturnFail_And_NotPersist_When_Name_Invalid(string? name)
        {
            // Arrange
            var repo = new FakeUserRepository();
            var handler = new RegisterUserHandler(repo);

            var command = new RegisterUserCommand
            {
                Name = name!
            };

            // Act
            var result = handler.Handle(command);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(Guid.Empty, result.UserId);
            Assert.Equal("INVALID_USER_NAME", result.ErrorCode);
            Assert.Empty(repo.Users);
        }

        [Fact]
        public void RegisterUserHandler_Should_Treat_DefaultCommand_As_Invalid()
        {
            // Arrange
            var repo = new FakeUserRepository();
            var handler = new RegisterUserHandler(repo);
            var command = new RegisterUserCommand(); // Name = string.Empty

            // Act
            var result = handler.Handle(command);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(Guid.Empty, result.UserId);
            Assert.Equal("INVALID_USER_NAME", result.ErrorCode);
            Assert.Empty(repo.Users);
        }

        #endregion

        #region RegisterUserHandler – cas nominal

        [Fact]
        public void RegisterUserHandler_Should_PersistUser_And_ReturnOk_When_Name_Valid()
        {
            // Arrange
            var repo = new FakeUserRepository();
            var handler = new RegisterUserHandler(repo);

            const string name = "Alice";

            var command = new RegisterUserCommand(name);

            // Act
            var result = handler.Handle(command);

            // Assert
            Assert.True(result.Success);
            Assert.NotEqual(Guid.Empty, result.UserId);
            Assert.Null(result.ErrorCode);

            Assert.Single(repo.Users);
            var stored = repo.Users[0];

            Assert.Equal(result.UserId, stored.Id);
            Assert.Equal(name.Trim(), stored.Name);
            Assert.Equal(0, stored.ActiveLoansCount);
            Assert.Equal(0m, stored.AmountDue);
        }

        #endregion

        #region RegisterUserHandler – cas improbables / bornes

        [Fact]
        public void RegisterUserHandler_Should_Handle_VeryLongName_Without_Exception()
        {
            // Arrange
            var repo = new FakeUserRepository();
            var handler = new RegisterUserHandler(repo);

            var veryLongName = new string('X', 10_000);
            var command = new RegisterUserCommand(veryLongName);

            // Act
            var exception = Record.Exception(() => handler.Handle(command));

            // Assert
            Assert.Null(exception);
            Assert.Single(repo.Users);

            var stored = repo.Users[0];
            Assert.Equal(veryLongName.Trim(), stored.Name);
        }

        [Fact]
        public void RegisterUserHandler_Should_Handle_Name_With_SpecialCharacters_And_Whitespace()
        {
            // Arrange
            var repo = new FakeUserRepository();
            var handler = new RegisterUserHandler(repo);

            const string rawName = "  \tBob 🚀 \n";
            var command = new RegisterUserCommand(rawName);

            // Act
            var result = handler.Handle(command);

            // Assert
            Assert.True(result.Success);
            Assert.Single(repo.Users);

            var stored = repo.Users[0];
            // on vérifie au moins que le nom n’est pas vide et que le trim basique fonctionne
            Assert.False(string.IsNullOrWhiteSpace(stored.Name));
        }

        [Fact]
        public void RegisterUserHandler_Should_Allow_DuplicateNames_With_DifferentIds()
        {
            // Arrange
            var repo = new FakeUserRepository();
            var handler = new RegisterUserHandler(repo);

            const string name = "DuplicateUser";

            var command1 = new RegisterUserCommand(name);
            var command2 = new RegisterUserCommand(name);

            // Act
            var result1 = handler.Handle(command1);
            var result2 = handler.Handle(command2);

            // Assert
            Assert.True(result1.Success);
            Assert.True(result2.Success);
            Assert.NotEqual(Guid.Empty, result1.UserId);
            Assert.NotEqual(Guid.Empty, result2.UserId);
            Assert.NotEqual(result1.UserId, result2.UserId);

            Assert.Equal(2, repo.Users.Count);
            Assert.All(repo.Users, u =>
            {
                Assert.Equal(name, u.Name);
                Assert.Equal(0, u.ActiveLoansCount);
                Assert.Equal(0m, u.AmountDue);
            });
        }

        #endregion
    }
}
