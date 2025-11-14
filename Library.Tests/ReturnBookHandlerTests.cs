using System;
using System.Collections.Generic;
using Library.Application;
using Library.Domain;
using Xunit;

namespace Library.Tests;

public class ReturnBookHandlerTests
{
    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly Dictionary<Guid, UserAccount> _users = new();

        public void Add(UserAccount user) => _users[user.Id] = user;

        public UserAccount? GetById(Guid id) =>
            _users.TryGetValue(id, out var user) ? user : null;
    }

    private sealed class FakeBookCopyRepository : IBookCopyRepository
    {
        private readonly List<BookCopy> _copies = new();

        public void Add(BookCopy copy) => _copies.Add(copy);

        public BookCopy? FindAvailableCopy(Guid bookId, Guid siteId)
        {
            foreach (var copy in _copies)
            {
                if (copy.BookId == bookId &&
                    copy.SiteId == siteId &&
                    copy.Status == BookCopyStatus.Available)
                {
                    return copy;
                }
            }
            return null;
        }

        public BookCopy? GetById(Guid id)
        {
            foreach (var copy in _copies)
            {
                if (copy.Id == id)
                    return copy;
            }
            return null;
        }
    }

    private sealed class FakeLoanRepository : ILoanRepository
    {
        private readonly List<Loan> _loans = new();

        public void Add(Loan loan) => _loans.Add(loan);

        public Loan? GetById(Guid id)
        {
            foreach (var loan in _loans)
            {
                if (loan.Id == id)
                    return loan;
            }
            return null;
        }

        public IReadOnlyList<Loan> Loans => _loans.AsReadOnly();
    }

    private sealed class FakeClock : IClock
    {
        public DateTime Now { get; set; }

        public DateTime UtcNow => Now;
    }

    [Theory]
    // borrowedAt        dueDate          returnDate        initialAmount    expectedAmount
    [InlineData(2025, 1, 1, 2025, 1, 10, 2025, 1, 10, 0.0, 0.0)] // pas de retard
    [InlineData(2025, 1, 1, 2025, 1, 10, 2025, 1, 12, 1.0, 2.0)] // 2j retard * 0.5 = +1
    public void Handle_successful_return_updates_user_loan_copy_and_penalties(
        int bYear, int bMonth, int bDay,
        int dYear, int dMonth, int dDay,
        int rYear, int rMonth, int rDay,
        decimal initialAmountDue,
        decimal expectedAmountDue)
    {
        // Arrange
        var userRepo = new FakeUserRepository();
        var copyRepo = new FakeBookCopyRepository();
        var loanRepo = new FakeLoanRepository();
        var clock = new FakeClock { Now = new DateTime(rYear, rMonth, rDay) };

        var penaltyService = new PenaltyService();
        var returnService = new ReturnService(penaltyService);

        var handler = new ReturnBookHandler(userRepo, copyRepo, loanRepo, clock, returnService);

        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var siteId = Guid.NewGuid();

        var user = new UserAccount(userId, "Test user", activeLoansCount: 1, amountDue: initialAmountDue);
        userRepo.Add(user);

        var copy = new BookCopy(bookId, siteId);
        copy.MarkAsBorrowed();
        copyRepo.Add(copy);

        var borrowedAt = new DateTime(bYear, bMonth, bDay);
        var dueDate = new DateTime(dYear, dMonth, dDay);

        var loan = new Loan(userId, copy.Id, borrowedAt, dueDate);
        loanRepo.Add(loan);

        var command = new ReturnBookCommand(loan.Id);

        // Act
        var result = handler.Handle(command);

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.ErrorCode);

        Assert.Equal(expectedAmountDue, user.AmountDue);
        Assert.Equal(0, user.ActiveLoansCount);
        Assert.Equal(BookCopyStatus.Available, copy.Status);
        Assert.Equal(clock.Now, loan.ReturnedAt);
    }

    [Fact]
    public void Handle_returns_error_when_loan_not_found()
    {
        // Arrange
        var userRepo = new FakeUserRepository();
        var copyRepo = new FakeBookCopyRepository();
        var loanRepo = new FakeLoanRepository();
        var clock = new FakeClock { Now = new DateTime(2025, 1, 10) };

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
    public void Handle_returns_error_when_user_not_found()
    {
        // Arrange
        var userRepo = new FakeUserRepository(); // vide
        var copyRepo = new FakeBookCopyRepository();
        var loanRepo = new FakeLoanRepository();
        var clock = new FakeClock { Now = new DateTime(2025, 1, 12) };

        var penaltyService = new PenaltyService();
        var returnService = new ReturnService(penaltyService);

        var handler = new ReturnBookHandler(userRepo, copyRepo, loanRepo, clock, returnService);

        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var siteId = Guid.NewGuid();

        var copy = new BookCopy(bookId, siteId);
        copy.MarkAsBorrowed();
        copyRepo.Add(copy);

        var borrowedAt = new DateTime(2025, 1, 1);
        var dueDate = new DateTime(2025, 1, 10);
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
    public void Handle_returns_error_when_copy_not_found()
    {
        // Arrange
        var userRepo = new FakeUserRepository();
        var copyRepo = new FakeBookCopyRepository(); // pas de copy
        var loanRepo = new FakeLoanRepository();
        var clock = new FakeClock { Now = new DateTime(2025, 1, 12) };

        var penaltyService = new PenaltyService();
        var returnService = new ReturnService(penaltyService);

        var handler = new ReturnBookHandler(userRepo, copyRepo, loanRepo, clock, returnService);

        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var siteId = Guid.NewGuid();

        var user = new UserAccount(userId, "Test user", activeLoansCount: 1, amountDue: 0m);
        userRepo.Add(user);

        var borrowedAt = new DateTime(2025, 1, 1);
        var dueDate = new DateTime(2025, 1, 10);

        // Loan pointe vers un copyId qui n’existe pas dans le repo
        var fakeCopyId = Guid.NewGuid();
        var loan = new Loan(userId, fakeCopyId, borrowedAt, dueDate);
        loanRepo.Add(loan);

        var command = new ReturnBookCommand(loan.Id);

        // Act
        var result = handler.Handle(command);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("COPY_NOT_FOUND", result.ErrorCode);
    }
}
