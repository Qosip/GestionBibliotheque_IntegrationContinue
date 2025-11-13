using System;
using System.Collections.Generic;
using Library.Application;
using Library.Domain;
using Xunit;

namespace Library.Tests;

public class BorrowBookHandlerTests
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
    }

    private sealed class FakeLoanRepository : ILoanRepository
    {
        private readonly List<Loan> _loans = new();

        public void Add(Loan loan) => _loans.Add(loan);

        public IReadOnlyList<Loan> Loans => _loans.AsReadOnly();
    }

    private sealed class FakeClock : IClock
    {
        public DateTime Now { get; set; }

        public DateTime UtcNow => Now;
    }

    [Fact]
    public void Handle_successful_borrow_creates_loan_and_returns_success()
    {
        // Arrange
        var userRepo = new FakeUserRepository();
        var copyRepo = new FakeBookCopyRepository();
        var loanRepo = new FakeLoanRepository();
        var clock = new FakeClock { Now = new DateTime(2025, 1, 1) };
        var borrowSvc = new BorrowingService();

        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var siteId = Guid.NewGuid();

        var user = new UserAccount(userId, activeLoansCount: 0, amountDue: 0m);
        userRepo.Add(user);

        var copy = new BookCopy(bookId, siteId);
        copyRepo.Add(copy);

        var handler = new BorrowBookHandler(userRepo, copyRepo, loanRepo, clock, borrowSvc);

        var command = new BorrowBookCommand(userId, bookId, siteId);

        // Act
        var result = handler.Handle(command);

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.ErrorCode);
        Assert.NotEqual(Guid.Empty, result.LoanId);

        Assert.Single(loanRepo.Loans);
        var loan = loanRepo.Loans[0];
        Assert.Equal(userId, loan.UserAccountId);
        Assert.Equal(copy.Id, loan.BookCopyId);
        Assert.Equal(clock.Now, loan.BorrowedAt);
        Assert.Equal(clock.Now.AddDays(14), loan.DueDate); // règle : 14 jours d’emprunt
    }

    [Fact]
    public void Handle_returns_error_when_user_not_found()
    {
        // Arrange
        var userRepo = new FakeUserRepository(); // vide
        var copyRepo = new FakeBookCopyRepository();
        var loanRepo = new FakeLoanRepository();
        var clock = new FakeClock { Now = new DateTime(2025, 1, 1) };
        var borrowSvc = new BorrowingService();

        var command = new BorrowBookCommand(
            userId: Guid.NewGuid(),
            bookId: Guid.NewGuid(),
            siteId: Guid.NewGuid());

        var handler = new BorrowBookHandler(userRepo, copyRepo, loanRepo, clock, borrowSvc);

        // Act
        var result = handler.Handle(command);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("USER_NOT_FOUND", result.ErrorCode);
        Assert.Equal(Guid.Empty, result.LoanId);
        Assert.Empty(loanRepo.Loans);
    }

    [Fact]
    public void Handle_returns_error_when_no_copy_available_for_site()
    {
        // Arrange
        var userRepo = new FakeUserRepository();
        var copyRepo = new FakeBookCopyRepository();
        var loanRepo = new FakeLoanRepository();
        var clock = new FakeClock { Now = new DateTime(2025, 1, 1) };
        var borrowSvc = new BorrowingService();

        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var siteId = Guid.NewGuid();

        var user = new UserAccount(userId, activeLoansCount: 0, amountDue: 0m);
        userRepo.Add(user);

        // On ajoute un exemplaire mais sur un autre site -> pas dispo pour ce site
        var otherSiteId = Guid.NewGuid();
        var copy = new BookCopy(bookId, otherSiteId);
        copyRepo.Add(copy);

        var handler = new BorrowBookHandler(userRepo, copyRepo, loanRepo, clock, borrowSvc);

        var command = new BorrowBookCommand(userId, bookId, siteId);

        // Act
        var result = handler.Handle(command);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("NO_COPY_AVAILABLE_AT_SITE", result.ErrorCode);
        Assert.Equal(Guid.Empty, result.LoanId);
        Assert.Empty(loanRepo.Loans);
    }
}
