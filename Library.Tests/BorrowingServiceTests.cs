using System;
using Library.Domain;
using Xunit;

namespace Library.Tests;

public class BorrowingServiceTests
{
    [Theory]
    // activeLoans,         copyStatus,                  expectedSuccess, expectedErrorCode
    [InlineData(4, BookCopyStatus.Available, true, null)]
    [InlineData(5, BookCopyStatus.Available, false, "BORROW_LIMIT_REACHED")]
    [InlineData(0, BookCopyStatus.Borrowed, false, "COPY_NOT_AVAILABLE")]
    [InlineData(5, BookCopyStatus.Borrowed, false, "BORROW_LIMIT_REACHED")] // priorité au plafond utilisateur
    public void TryBorrow_enforces_user_limit_and_copy_availability(
        int activeLoans,
        BookCopyStatus copyStatus,
        bool expectedSuccess,
        string? expectedErrorCode)
    {
        // Arrange
        var user = new UserAccount(Guid.NewGuid(), activeLoans, amountDue: 0m);

        var bookId = Guid.NewGuid();
        var siteId = Guid.NewGuid();
        var copy = new BookCopy(bookId, siteId);

        if (copyStatus == BookCopyStatus.Borrowed)
        {
            copy.MarkAsBorrowed();
        }

        var borrowedAt = new DateTime(2025, 1, 1);
        var dueDate = new DateTime(2025, 1, 15);

        var service = new BorrowingService();

        var initialLoansCount = user.ActiveLoansCount;
        var initialCopyStatus = copy.Status;

        // Act
        var result = service.TryBorrow(user, copy, borrowedAt, dueDate);

        // Assert
        Assert.Equal(expectedSuccess, result.Success);
        Assert.Equal(expectedErrorCode, result.ErrorCode);

        if (expectedSuccess)
        {
            Assert.Equal(initialLoansCount + 1, user.ActiveLoansCount);
            Assert.Equal(BookCopyStatus.Borrowed, copy.Status);
        }
        else
        {
            Assert.Equal(initialLoansCount, user.ActiveLoansCount);
            Assert.Equal(initialCopyStatus, copy.Status);
        }
    }

    [Fact]
    public void TryBorrow_throws_when_due_date_before_borrowed_date()
    {
        // Arrange
        var user = new UserAccount(Guid.NewGuid(), activeLoansCount: 0, amountDue: 0m);
        var copy = new BookCopy(Guid.NewGuid(), Guid.NewGuid());

        var borrowedAt = new DateTime(2025, 1, 10);
        var dueDate = new DateTime(2025, 1, 5);

        var service = new BorrowingService();

        // Act + Assert
        Assert.Throws<ArgumentException>(() =>
        {
            service.TryBorrow(user, copy, borrowedAt, dueDate);
        });
    }
}
