using System;
using Library.Domain;
using Xunit;

namespace Library.Tests;

public class UserAccountBalanceTests
{
    [Fact]
    public void AddAmountDue_increases_AmountDue_when_positive()
    {
        // Arrange
        var user = new UserAccount(Guid.NewGuid(), "Test user", activeLoansCount: 0, amountDue: 10m);

        // Act
        user.AddAmount(2.5m);

        // Assert
        Assert.Equal(12.5m, user.AmountDue);
    }

    [Fact]
    public void AddAmountDue_does_nothing_when_zero()
    {
        // Arrange
        var user = new UserAccount(Guid.NewGuid(), "Test user", activeLoansCount: 1, amountDue: 5m);

        // Act
        user.AddAmount(0m);

        // Assert
        Assert.Equal(5m, user.AmountDue);
    }

    [Fact]
    public void AddAmountDue_throws_when_negative()
    {
        // Arrange
        var user = new UserAccount(Guid.NewGuid(), "Test user", activeLoansCount: 0, amountDue: 0m);

        // Act + Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            user.AddAmount(-1m);
        });
    }
}
