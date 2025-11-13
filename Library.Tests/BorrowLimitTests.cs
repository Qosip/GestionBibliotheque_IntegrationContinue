using System;
using Library.Domain;
using Xunit;

namespace Library.Tests;

public class BorrowLimitTests
{
    [Fact]
    public void Cannot_borrow_more_than_5_books()
    {
        // Arrange
        var user = new UserAccount(Guid.NewGuid(), activeLoansCount: 5);
        var service = new BorrowingService();

        // Act
        var result = service.TryBorrow(user);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("BORROW_LIMIT_REACHED", result.ErrorCode);
    }

    [Fact]
    public void Can_borrow_when_user_has_less_than_5_books()
    {
        // Arrange
        var user = new UserAccount(Guid.NewGuid(), activeLoansCount: 4);
        var service = new BorrowingService();

        // Act
        var result = service.TryBorrow(user);

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.ErrorCode);
    }
}
