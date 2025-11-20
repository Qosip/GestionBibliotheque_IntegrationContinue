using System;
using System.Collections.Generic;
using System.Linq;
using Library.Application;
using Library.Application.Commands;
using Library.Application.Handlers;
using Library.Application.Repositories;
using Library.Application.Results;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Library.Domain.Services;
using Xunit;

namespace Library.Tests.Application
{
    public sealed class ApplicationLoanTests
    {
        #region Fakes

        private sealed class FakeClock : IClock
        {
            public DateTime UtcNow { get; set; }
        }

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

        private sealed class FakeBookCopyRepository : IBookCopyRepository
        {
            private readonly List<BookCopy> _copies = new();

            public IReadOnlyList<BookCopy> Copies => _copies.AsReadOnly();

            public BookCopy? GetById(Guid id) =>
                _copies.SingleOrDefault(c => c.Id == id);

            public BookCopy? FindAvailableCopy(Guid bookId, Guid siteId) =>
                _copies.FirstOrDefault(c =>
                    c.BookId == bookId &&
                    c.SiteId == siteId &&
                    c.Status == BookCopyStatus.Available);

            public IEnumerable<BookCopy> GetByBook(Guid bookId) =>
                _copies.Where(c => c.BookId == bookId).ToList();

            public void Add(BookCopy copy)
            {
                if (copy == null) throw new ArgumentNullException(nameof(copy));
                _copies.Add(copy);
            }

            public void Update(BookCopy copy)
            {
                if (copy == null) throw new ArgumentNullException(nameof(copy));

                var index = _copies.FindIndex(c => c.Id == copy.Id);
                if (index >= 0)
                {
                    _copies[index] = copy;
                }
            }
        }

        private sealed class FakeLoanRepository : ILoanRepository
        {
            private readonly List<Loan> _loans = new();

            public IReadOnlyList<Loan> Loans => _loans.AsReadOnly();

            public Loan? GetById(Guid id) =>
                _loans.SingleOrDefault(l => l.Id == id);

            public IEnumerable<Loan> GetActiveLoansForUser(Guid userId) =>
                _loans.Where(l => l.UserAccountId == userId && !l.ReturnedAt.HasValue).ToList();

            public Loan? GetActiveLoanForUserAndCopy(Guid userId, Guid bookCopyId) =>
                _loans.FirstOrDefault(l =>
                    l.UserAccountId == userId &&
                    l.BookCopyId == bookCopyId &&
                    !l.ReturnedAt.HasValue);

            public void Add(Loan loan)
            {
                if (loan == null) throw new ArgumentNullException(nameof(loan));
                _loans.Add(loan);
            }

            public void Update(Loan loan)
            {
                if (loan == null) throw new ArgumentNullException(nameof(loan));

                var index = _loans.FindIndex(l => l.Id == loan.Id);
                if (index >= 0)
                {
                    _loans[index] = loan;
                }
            }
        }

        #endregion

        #region BorrowBookResult

        [Fact]
        public void BorrowBookResult_Ok_Should_SetSuccess_LoanId_AndNullErrorCode()
        {
            // Intention : vérifier que le factory Ok positionne un succès avec un Id de prêt valide.
            // Arrange
            var loanId = Guid.NewGuid();

            // Act
            var result = BorrowBookResult.Ok(loanId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(loanId, result.LoanId);
            Assert.Null(result.ErrorCode);
        }

        [Fact]
        public void BorrowBookResult_Fail_Should_SetFailure_EmptyLoanId_AndErrorCode()
        {
            // Intention : vérifier que le factory Fail renseigne bien l’erreur et réinitialise l’Id.
            // Arrange
            const string errorCode = "ANY_ERROR";

            // Act
            var result = BorrowBookResult.Fail(errorCode);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(Guid.Empty, result.LoanId);
            Assert.Equal(errorCode, result.ErrorCode);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void BorrowBookResult_Fail_Should_Tolerate_NullOrWhitespace_ErrorCode(string? errorCode)
        {
            // Intention : s’assurer que Fail ne jette pas même si le code d’erreur est peu exploitable.
            // Arrange
            // Act
            var result = BorrowBookResult.Fail(errorCode!);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(Guid.Empty, result.LoanId);
            Assert.Equal(errorCode, result.ErrorCode);
        }

        #endregion

        #region BorrowBookCommand

        [Fact]
        public void BorrowBookCommand_ParameterlessCtor_Should_Default_Ids_To_Empty()
        {
            // Intention : vérifier les valeurs par défaut de la commande sans paramètre.
            // Arrange
            // Act
            var command = new BorrowBookCommand();

            // Assert
            Assert.Equal(Guid.Empty, command.UserId);
            Assert.Equal(Guid.Empty, command.BookId);
            Assert.Equal(Guid.Empty, command.SiteId);
        }

        [Fact]
        public void BorrowBookCommand_ParameterizedCtor_Should_Set_Properties()
        {
            // Intention : vérifier que le constructeur paramétré recopie bien les arguments.
            // Arrange
            var userId = Guid.NewGuid();
            var bookId = Guid.NewGuid();
            var siteId = Guid.NewGuid();

            // Act
            var command = new BorrowBookCommand(userId, bookId, siteId);

            // Assert
            Assert.Equal(userId, command.UserId);
            Assert.Equal(bookId, command.BookId);
            Assert.Equal(siteId, command.SiteId);
        }

        [Fact]
        public void BorrowBookCommand_Properties_Should_Be_Mutable()
        {
            // Intention : vérifier que les propriétés restent modifiables après construction.
            // Arrange
            var command = new BorrowBookCommand();

            var userId = Guid.NewGuid();
            var bookId = Guid.NewGuid();
            var siteId = Guid.NewGuid();

            // Act
            command.UserId = userId;
            command.BookId = bookId;
            command.SiteId = siteId;

            // Assert
            Assert.Equal(userId, command.UserId);
            Assert.Equal(bookId, command.BookId);
            Assert.Equal(siteId, command.SiteId);
        }

        #endregion

        #region BorrowBookHandler – validation / erreurs

        [Fact]
        public void BorrowBookHandler_Handle_Should_Throw_When_Command_Is_Null()
        {
            // Intention : s’assurer que le handler signale clairement une commande null.
            // Arrange
            var userRepo = new FakeUserRepository();
            var copyRepo = new FakeBookCopyRepository();
            var loanRepo = new FakeLoanRepository();
            var clock = new FakeClock { UtcNow = DateTime.UtcNow };
            var borrowingService = new BorrowingService();

            var handler = new BorrowBookHandler(userRepo, copyRepo, loanRepo, clock, borrowingService);

            // Act
            var exception = Assert.Throws<ArgumentNullException>(() => handler.Handle(null!));

            // Assert
            Assert.Equal("command", exception.ParamName);
        }

        [Fact]
        public void BorrowBookHandler_Should_Fail_When_User_Not_Found()
        {
            // Intention : vérifier que l’absence d’utilisateur remonte un code d’erreur dédié.
            // Arrange
            var userRepo = new FakeUserRepository(); // vide
            var copyRepo = new FakeBookCopyRepository();
            var loanRepo = new FakeLoanRepository();
            var clock = new FakeClock { UtcNow = new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc) };
            var borrowingService = new BorrowingService();

            // on prépare quand même une copy pour vérifier qu’elle n’est pas touchée
            var bookId = Guid.NewGuid();
            var siteId = Guid.NewGuid();
            var copy = new BookCopy(bookId, siteId);
            copyRepo.Add(copy);

            var command = new BorrowBookCommand(Guid.NewGuid(), bookId, siteId);
            var handler = new BorrowBookHandler(userRepo, copyRepo, loanRepo, clock, borrowingService);

            // Act
            var result = handler.Handle(command);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(Guid.Empty, result.LoanId);
            Assert.Equal("USER_NOT_FOUND", result.ErrorCode);
            Assert.Empty(loanRepo.Loans); // aucun prêt créé
        }

        [Fact]
        public void BorrowBookHandler_Should_Fail_When_No_Copy_Available_At_Site()
        {
            // Intention : vérifier que l’absence de copy disponible retourne le code attendu.
            // Arrange
            var userRepo = new FakeUserRepository();
            var copyRepo = new FakeBookCopyRepository(); // aucune copy
            var loanRepo = new FakeLoanRepository();
            var clock = new FakeClock { UtcNow = new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc) };
            var borrowingService = new BorrowingService();

            var user = new UserAccount(Guid.NewGuid(), "User", activeLoansCount: 0, amountDue: 0m);
            userRepo.Add(user);

            var bookId = Guid.NewGuid();
            var siteId = Guid.NewGuid();

            var command = new BorrowBookCommand(user.Id, bookId, siteId);
            var handler = new BorrowBookHandler(userRepo, copyRepo, loanRepo, clock, borrowingService);

            // Act
            var result = handler.Handle(command);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(Guid.Empty, result.LoanId);
            Assert.Equal("NO_COPY_AVAILABLE_AT_SITE", result.ErrorCode);
            Assert.Empty(loanRepo.Loans);
        }

        [Fact]
        public void BorrowBookHandler_Should_Fail_When_User_Reached_MaxActiveLoans()
        {
            // Intention : vérifier la propagation de l’erreur BORROW_LIMIT_REACHED depuis le service métier.
            // Arrange
            var userRepo = new FakeUserRepository();
            var copyRepo = new FakeBookCopyRepository();
            var loanRepo = new FakeLoanRepository();
            var clock = new FakeClock { UtcNow = new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc) };
            var borrowingService = new BorrowingService();

            var user = new UserAccount(Guid.NewGuid(), "User-limité", activeLoansCount: 5, amountDue: 0m);
            userRepo.Add(user);

            var bookId = Guid.NewGuid();
            var siteId = Guid.NewGuid();
            var copy = new BookCopy(bookId, siteId); // Available
            copyRepo.Add(copy);

            var command = new BorrowBookCommand(user.Id, bookId, siteId);
            var handler = new BorrowBookHandler(userRepo, copyRepo, loanRepo, clock, borrowingService);

            // Act
            var result = handler.Handle(command);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(Guid.Empty, result.LoanId);
            Assert.Equal("BORROW_LIMIT_REACHED", result.ErrorCode);
            Assert.Empty(loanRepo.Loans);
            Assert.Equal(BookCopyStatus.Available, copy.Status); // pas de mutation de state
        }

        [Fact]
        public void BorrowBookHandler_Should_Fail_With_NoCopy_When_BookId_And_SiteId_Are_Empty()
        {
            // Intention : tester un cas aux bornes où l’on passe des GUID vides pour le livre et le site.
            // Arrange
            var userRepo = new FakeUserRepository();
            var copyRepo = new FakeBookCopyRepository();
            var loanRepo = new FakeLoanRepository();
            var clock = new FakeClock { UtcNow = new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc) };
            var borrowingService = new BorrowingService();

            var user = new UserAccount(Guid.NewGuid(), "User", activeLoansCount: 0, amountDue: 0m);
            userRepo.Add(user);

            var command = new BorrowBookCommand(user.Id, Guid.Empty, Guid.Empty);
            var handler = new BorrowBookHandler(userRepo, copyRepo, loanRepo, clock, borrowingService);

            // Act
            var result = handler.Handle(command);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("NO_COPY_AVAILABLE_AT_SITE", result.ErrorCode);
            Assert.Empty(loanRepo.Loans);
        }

        [Fact]
        public void BorrowBookHandler_Should_Throw_When_Clock_Is_At_MaxValue_And_DueDate_Overflows()
        {
            // Intention : valider un scénario extrême où l’addition de jours dépasse la limite de DateTime.
            // Arrange
            var userRepo = new FakeUserRepository();
            var copyRepo = new FakeBookCopyRepository();
            var loanRepo = new FakeLoanRepository();
            var clock = new FakeClock { UtcNow = DateTime.MaxValue }; // provoquera AddDays overflow
            var borrowingService = new BorrowingService();

            var user = new UserAccount(Guid.NewGuid(), "EdgeUser", activeLoansCount: 0, amountDue: 0m);
            userRepo.Add(user);

            var bookId = Guid.NewGuid();
            var siteId = Guid.NewGuid();
            var copy = new BookCopy(bookId, siteId);
            copyRepo.Add(copy);

            var command = new BorrowBookCommand(user.Id, bookId, siteId);
            var handler = new BorrowBookHandler(userRepo, copyRepo, loanRepo, clock, borrowingService);

            // Act
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => handler.Handle(command));

            // Assert
            Assert.Empty(loanRepo.Loans);
            Assert.Equal(BookCopyStatus.Available, copy.Status);
        }

        #endregion

        #region BorrowBookHandler – cas nominal

        [Fact]
        public void BorrowBookHandler_Should_CreateLoan_And_Update_User_And_Copy_When_Data_Valid()
        {
            // Intention : valider le scénario nominal complet d’emprunt.
            // Arrange
            var userRepo = new FakeUserRepository();
            var copyRepo = new FakeBookCopyRepository();
            var loanRepo = new FakeLoanRepository();
            var clock = new FakeClock { UtcNow = new DateTime(2030, 1, 1, 10, 0, 0, DateTimeKind.Utc) };
            var borrowingService = new BorrowingService();

            var user = new UserAccount(Guid.NewGuid(), "User", activeLoansCount: 0, amountDue: 0m);
            userRepo.Add(user);

            var bookId = Guid.NewGuid();
            var siteId = Guid.NewGuid();
            var copy = new BookCopy(bookId, siteId); // Available
            copyRepo.Add(copy);

            var command = new BorrowBookCommand(user.Id, bookId, siteId);
            var handler = new BorrowBookHandler(userRepo, copyRepo, loanRepo, clock, borrowingService);

            // Act
            var result = handler.Handle(command);

            // Assert
            Assert.True(result.Success);
            Assert.NotEqual(Guid.Empty, result.LoanId);
            Assert.Null(result.ErrorCode);
            Assert.Single(loanRepo.Loans);

            var loan = loanRepo.Loans[0];
            Assert.Equal(result.LoanId, loan.Id);
            Assert.Equal(user.Id, loan.UserAccountId);
            Assert.Equal(copy.Id, loan.BookCopyId);
            Assert.Equal(clock.UtcNow, loan.BorrowedAt);
            Assert.Equal(clock.UtcNow.AddDays(14), loan.DueDate);

            var storedUser = userRepo.Users[0];
            Assert.Equal(1, storedUser.ActiveLoansCount);
            Assert.Equal(0m, storedUser.AmountDue);

            var storedCopy = copyRepo.Copies[0];
            Assert.Equal(BookCopyStatus.Borrowed, storedCopy.Status);
        }

        #endregion

        #region ReturnBookResult

        [Fact]
        public void ReturnBookResult_Ok_Should_SetSuccess_AndNullErrorCode()
        {
            // Intention : vérifier le factory Ok de résultat de retour.
            // Arrange
            // Act
            var result = ReturnBookResult.Ok();

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.ErrorCode);
        }

        [Fact]
        public void ReturnBookResult_Fail_Should_SetFailure_AndErrorCode()
        {
            // Intention : vérifier le factory Fail de résultat de retour.
            // Arrange
            const string errorCode = "ANY_ERROR";

            // Act
            var result = ReturnBookResult.Fail(errorCode);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(errorCode, result.ErrorCode);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ReturnBookResult_Fail_Should_Tolerate_NullOrWhitespace_ErrorCode(string? errorCode)
        {
            // Intention : s’assurer que Fail ne jette pas même avec un code peu informatif.
            // Arrange
            // Act
            var result = ReturnBookResult.Fail(errorCode!);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(errorCode, result.ErrorCode);
        }

        #endregion

        #region ReturnBookCommand

        [Fact]
        public void ReturnBookCommand_ParameterlessCtor_Should_Default_LoanId_To_Empty()
        {
            // Intention : vérifier la valeur par défaut de LoanId.
            // Arrange
            // Act
            var command = new ReturnBookCommand();

            // Assert
            Assert.Equal(Guid.Empty, command.LoanId);
        }

        [Fact]
        public void ReturnBookCommand_ParameterizedCtor_Should_Set_LoanId()
        {
            // Intention : vérifier le constructeur paramétré.
            // Arrange
            var loanId = Guid.NewGuid();

            // Act
            var command = new ReturnBookCommand(loanId);

            // Assert
            Assert.Equal(loanId, command.LoanId);
        }

        [Fact]
        public void ReturnBookCommand_LoanId_Should_Be_Mutable()
        {
            // Intention : vérifier que la propriété LoanId est modifiable.
            // Arrange
            var command = new ReturnBookCommand();
            var loanId = Guid.NewGuid();

            // Act
            command.LoanId = loanId;

            // Assert
            Assert.Equal(loanId, command.LoanId);
        }

        #endregion

        #region ReturnBookHandler – construction / validation

        [Fact]
        public void ReturnBookHandler_Ctor_Should_Throw_When_UserRepository_Is_Null()
        {
            // Intention : s’assurer que le handler refuse une dépendance null (userRepository).
            // Arrange
            IUserRepository? userRepo = null;
            var copyRepo = new FakeBookCopyRepository();
            var loanRepo = new FakeLoanRepository();
            var clock = new FakeClock();
            var penaltyService = new PenaltyService();
            var returnService = new ReturnService(penaltyService);

            // Act
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ReturnBookHandler(userRepo!, copyRepo, loanRepo, clock, returnService));

            // Assert
            Assert.Equal("userRepository", exception.ParamName);
        }

        [Fact]
        public void ReturnBookHandler_Ctor_Should_Throw_When_BookCopyRepository_Is_Null()
        {
            // Intention : s’assurer que le handler refuse bookCopyRepository null.
            // Arrange
            var userRepo = new FakeUserRepository();
            IBookCopyRepository? copyRepo = null;
            var loanRepo = new FakeLoanRepository();
            var clock = new FakeClock();
            var penaltyService = new PenaltyService();
            var returnService = new ReturnService(penaltyService);

            // Act
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ReturnBookHandler(userRepo, copyRepo!, loanRepo, clock, returnService));

            // Assert
            Assert.Equal("bookCopyRepository", exception.ParamName);
        }

        [Fact]
        public void ReturnBookHandler_Ctor_Should_Throw_When_LoanRepository_Is_Null()
        {
            // Intention : s’assurer que le handler refuse loanRepository null.
            // Arrange
            var userRepo = new FakeUserRepository();
            var copyRepo = new FakeBookCopyRepository();
            ILoanRepository? loanRepo = null;
            var clock = new FakeClock();
            var penaltyService = new PenaltyService();
            var returnService = new ReturnService(penaltyService);

            // Act
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ReturnBookHandler(userRepo, copyRepo, loanRepo!, clock, returnService));

            // Assert
            Assert.Equal("loanRepository", exception.ParamName);
        }

        [Fact]
        public void ReturnBookHandler_Ctor_Should_Throw_When_Clock_Is_Null()
        {
            // Intention : s’assurer que le handler refuse clock null.
            // Arrange
            var userRepo = new FakeUserRepository();
            var copyRepo = new FakeBookCopyRepository();
            var loanRepo = new FakeLoanRepository();
            IClock? clock = null;
            var penaltyService = new PenaltyService();
            var returnService = new ReturnService(penaltyService);

            // Act
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ReturnBookHandler(userRepo, copyRepo, loanRepo, clock!, returnService));

            // Assert
            Assert.Equal("clock", exception.ParamName);
        }

        [Fact]
        public void ReturnBookHandler_Ctor_Should_Throw_When_ReturnService_Is_Null()
        {
            // Intention : s’assurer que le handler refuse returnService null.
            // Arrange
            var userRepo = new FakeUserRepository();
            var copyRepo = new FakeBookCopyRepository();
            var loanRepo = new FakeLoanRepository();
            var clock = new FakeClock();
            ReturnService? returnService = null;

            // Act
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ReturnBookHandler(userRepo, copyRepo, loanRepo, clock, returnService!));

            // Assert
            Assert.Equal("returnService", exception.ParamName);
        }

        [Fact]
        public void ReturnBookHandler_Handle_Should_Throw_When_Command_Is_Null()
        {
            // Intention : vérifier que Handle refuse une commande null.
            // Arrange
            var userRepo = new FakeUserRepository();
            var copyRepo = new FakeBookCopyRepository();
            var loanRepo = new FakeLoanRepository();
            var clock = new FakeClock { UtcNow = DateTime.UtcNow };
            var penaltyService = new PenaltyService();
            var returnService = new ReturnService(penaltyService);
            var handler = new ReturnBookHandler(userRepo, copyRepo, loanRepo, clock, returnService);

            // Act
            var exception = Assert.Throws<ArgumentNullException>(() => handler.Handle(null!));

            // Assert
            Assert.Equal("command", exception.ParamName);
        }

        #endregion

        #region ReturnBookHandler – erreurs

        [Fact]
        public void ReturnBookHandler_Should_Fail_When_Loan_Not_Found()
        {
            // Intention : vérifier que l’absence de prêt remonte LOAN_NOT_FOUND.
            // Arrange
            var userRepo = new FakeUserRepository();
            var copyRepo = new FakeBookCopyRepository();
            var loanRepo = new FakeLoanRepository(); // aucun prêt
            var clock = new FakeClock { UtcNow = new DateTime(2030, 1, 10, 10, 0, 0, DateTimeKind.Utc) };
            var penaltyService = new PenaltyService();
            var returnService = new ReturnService(penaltyService);
            var handler = new ReturnBookHandler(userRepo, copyRepo, loanRepo, clock, returnService);

            var command = new ReturnBookCommand(Guid.NewGuid());

            // Act
            var result = handler.Handle(command);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("LOAN_NOT_FOUND", result.ErrorCode);
        }

        [Fact]
        public void ReturnBookHandler_Should_Fail_When_User_Not_Found()
        {
            // Intention : vérifier le cas où le prêt existe mais pas l’utilisateur associé.
            // Arrange
            var userRepo = new FakeUserRepository(); // vide
            var copyRepo = new FakeBookCopyRepository();
            var loanRepo = new FakeLoanRepository();
            var clock = new FakeClock { UtcNow = new DateTime(2030, 1, 10, 10, 0, 0, DateTimeKind.Utc) };
            var penaltyService = new PenaltyService();
            var returnService = new ReturnService(penaltyService);
            var handler = new ReturnBookHandler(userRepo, copyRepo, loanRepo, clock, returnService);

            var userId = Guid.NewGuid();
            var bookId = Guid.NewGuid();
            var siteId = Guid.NewGuid();
            var copy = new BookCopy(bookId, siteId);
            copy.MarkAsBorrowed();

            var borrowedAt = new DateTime(2030, 1, 1, 10, 0, 0, DateTimeKind.Utc);
            var dueDate = borrowedAt.AddDays(14);
            var loan = new Loan(userId, copy.Id, borrowedAt, dueDate);
            loanRepo.Add(loan);

            var command = new ReturnBookCommand(loan.Id);

            // Act
            var result = handler.Handle(command);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("USER_NOT_FOUND", result.ErrorCode);
        }

        [Fact]
        public void ReturnBookHandler_Should_Fail_When_Copy_Not_Found()
        {
            // Intention : vérifier le cas où le prêt et l’utilisateur existent mais pas la copy.
            // Arrange
            var userRepo = new FakeUserRepository();
            var copyRepo = new FakeBookCopyRepository(); // aucune copy
            var loanRepo = new FakeLoanRepository();
            var clock = new FakeClock { UtcNow = new DateTime(2030, 1, 10, 10, 0, 0, DateTimeKind.Utc) };
            var penaltyService = new PenaltyService();
            var returnService = new ReturnService(penaltyService);
            var handler = new ReturnBookHandler(userRepo, copyRepo, loanRepo, clock, returnService);

            var user = new UserAccount(Guid.NewGuid(), "User", activeLoansCount: 1, amountDue: 0m);
            userRepo.Add(user);

            var bookId = Guid.NewGuid();
            var siteId = Guid.NewGuid();

            var borrowedAt = new DateTime(2030, 1, 1, 10, 0, 0, DateTimeKind.Utc);
            var dueDate = borrowedAt.AddDays(14);
            var copyId = Guid.NewGuid(); // copy inexistante
            var loan = new Loan(user.Id, copyId, borrowedAt, dueDate);
            loanRepo.Add(loan);

            var command = new ReturnBookCommand(loan.Id);

            // Act
            var result = handler.Handle(command);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("COPY_NOT_FOUND", result.ErrorCode);
        }

        [Fact]
        public void ReturnBookHandler_Should_Throw_When_Loan_Already_Returned()
        {
            // Intention : tester un scénario incohérent où le prêt est déjà retourné avant l’appel.
            // Arrange
            var userRepo = new FakeUserRepository();
            var copyRepo = new FakeBookCopyRepository();
            var loanRepo = new FakeLoanRepository();
            var clock = new FakeClock { UtcNow = new DateTime(2030, 1, 20, 10, 0, 0, DateTimeKind.Utc) };
            var penaltyService = new PenaltyService();
            var returnService = new ReturnService(penaltyService);
            var handler = new ReturnBookHandler(userRepo, copyRepo, loanRepo, clock, returnService);

            var user = new UserAccount(Guid.NewGuid(), "User", activeLoansCount: 1, amountDue: 0m);
            userRepo.Add(user);

            var bookId = Guid.NewGuid();
            var siteId = Guid.NewGuid();
            var copy = new BookCopy(bookId, siteId);
            copy.MarkAsBorrowed();
            copyRepo.Add(copy);

            var borrowedAt = new DateTime(2030, 1, 1, 10, 0, 0, DateTimeKind.Utc);
            var dueDate = borrowedAt.AddDays(14);
            var loan = new Loan(user.Id, copy.Id, borrowedAt, dueDate);

            // prêt déjà retourné auparavant
            loan.MarkAsReturned(borrowedAt.AddDays(5));
            loanRepo.Add(loan);

            var command = new ReturnBookCommand(loan.Id);

            // Act
            var exception = Assert.Throws<InvalidOperationException>(() => handler.Handle(command));

            // Assert
            Assert.Equal("Loan is already returned.", exception.Message);
        }

        [Fact]
        public void ReturnBookHandler_Should_Throw_When_ReturnDate_Before_BorrowedAt()
        {
            // Intention : tester un cas extrême de “retour dans le passé” par rapport à BorrowedAt.
            // Arrange
            var userRepo = new FakeUserRepository();
            var copyRepo = new FakeBookCopyRepository();
            var loanRepo = new FakeLoanRepository();
            var clock = new FakeClock { UtcNow = new DateTime(2030, 1, 1, 10, 0, 0, DateTimeKind.Utc) }; // avant l’emprunt
            var penaltyService = new PenaltyService();
            var returnService = new ReturnService(penaltyService);
            var handler = new ReturnBookHandler(userRepo, copyRepo, loanRepo, clock, returnService);

            var user = new UserAccount(Guid.NewGuid(), "User", activeLoansCount: 1, amountDue: 0m);
            userRepo.Add(user);

            var bookId = Guid.NewGuid();
            var siteId = Guid.NewGuid();
            var copy = new BookCopy(bookId, siteId);
            copy.MarkAsBorrowed();
            copyRepo.Add(copy);

            var borrowedAt = new DateTime(2030, 1, 10, 10, 0, 0, DateTimeKind.Utc);
            var dueDate = borrowedAt.AddDays(14);
            var loan = new Loan(user.Id, copy.Id, borrowedAt, dueDate);
            loanRepo.Add(loan);

            var command = new ReturnBookCommand(loan.Id);

            // Act
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => handler.Handle(command));

            // Assert
            Assert.Equal("returnedAt", exception.ParamName);
        }


        #endregion

        #region ReturnBookHandler – cas nominaux (sans et avec retard)

        [Fact]
        public void ReturnBookHandler_Should_Return_Book_Without_Overdue_Penalty_When_OnTime()
        {
            // Intention : valider le scénario nominal sans retard (pas de pénalité).
            // Arrange
            var userRepo = new FakeUserRepository();
            var copyRepo = new FakeBookCopyRepository();
            var loanRepo = new FakeLoanRepository();
            var borrowedAt = new DateTime(2030, 1, 1, 10, 0, 0, DateTimeKind.Utc);
            var dueDate = borrowedAt.AddDays(14); // 15 janvier
            var returnDate = new DateTime(2030, 1, 14, 10, 0, 0, DateTimeKind.Utc); // avant ou à la limite

            var clock = new FakeClock { UtcNow = returnDate };
            var penaltyService = new PenaltyService();
            var returnService = new ReturnService(penaltyService);
            var handler = new ReturnBookHandler(userRepo, copyRepo, loanRepo, clock, returnService);

            var user = new UserAccount(Guid.NewGuid(), "User", activeLoansCount: 1, amountDue: 0m);
            userRepo.Add(user);

            var bookId = Guid.NewGuid();
            var siteId = Guid.NewGuid();
            var copy = new BookCopy(bookId, siteId);
            copy.MarkAsBorrowed();
            copyRepo.Add(copy);

            var loan = new Loan(user.Id, copy.Id, borrowedAt, dueDate);
            loanRepo.Add(loan);

            var command = new ReturnBookCommand(loan.Id);

            // Act
            var result = handler.Handle(command);

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.ErrorCode);

            Assert.NotNull(loan.ReturnedAt);
            Assert.Equal(returnDate, loan.ReturnedAt.Value);

            var storedUser = userRepo.Users[0];
            Assert.Equal(0, storedUser.ActiveLoansCount);
            Assert.Equal(0m, storedUser.AmountDue); // aucune pénalité

            var storedCopy = copyRepo.Copies[0];
            Assert.Equal(BookCopyStatus.Available, storedCopy.Status);
        }

        [Fact]
        public void ReturnBookHandler_Should_Apply_Overdue_Penalty_When_Returned_Late()
        {
            // Intention : valider l’application d’une pénalité en cas de retard.
            // Arrange
            var userRepo = new FakeUserRepository();
            var copyRepo = new FakeBookCopyRepository();
            var loanRepo = new FakeLoanRepository();

            var borrowedAt = new DateTime(2030, 1, 1, 10, 0, 0, DateTimeKind.Utc);
            var dueDate = borrowedAt.AddDays(7); // 8 janvier
            var returnDate = new DateTime(2030, 1, 11, 10, 0, 0, DateTimeKind.Utc); // 3 jours de retard

            var clock = new FakeClock { UtcNow = returnDate };
            var penaltyService = new PenaltyService();
            var returnService = new ReturnService(penaltyService);
            var handler = new ReturnBookHandler(userRepo, copyRepo, loanRepo, clock, returnService);

            var user = new UserAccount(Guid.NewGuid(), "User", activeLoansCount: 1, amountDue: 0m);
            userRepo.Add(user);

            var bookId = Guid.NewGuid();
            var siteId = Guid.NewGuid();
            var copy = new BookCopy(bookId, siteId);
            copy.MarkAsBorrowed();
            copyRepo.Add(copy);

            var loan = new Loan(user.Id, copy.Id, borrowedAt, dueDate);
            loanRepo.Add(loan);

            var command = new ReturnBookCommand(loan.Id);

            // Act
            var result = handler.Handle(command);

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.ErrorCode);

            var storedUser = userRepo.Users[0];
            Assert.Equal(0, storedUser.ActiveLoansCount);

            // 3 jours de retard à 0.5m / jour = 1.5m
            Assert.Equal(1.5m, storedUser.AmountDue);

            var storedCopy = copyRepo.Copies[0];
            Assert.Equal(BookCopyStatus.Available, storedCopy.Status);
        }

        #endregion
    }
}
