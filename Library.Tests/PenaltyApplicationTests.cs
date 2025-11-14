using System;
using Library.Domain;
using Xunit;

namespace Library.Tests;

public class PenaltyApplicationTests
{
    [Fact]
    public void ApplyOverduePenalty_updates_user_AmountDue_when_loan_is_overdue()
    {
        // Arrange
        var user = new UserAccount(Guid.NewGuid(), "Test user", activeLoansCount: 1, amountDue: 0m);
        var borrowedAt = new DateTime(2025, 1, 1);
        var dueDate = new DateTime(2025, 1, 10);
        var now = new DateTime(2025, 1, 13); // 3 jours de retard
        var loan = new Loan(borrowedAt, dueDate);
        var service = new PenaltyService();
        var dailyRate = 0.5m;

        // Act
        service.ApplyOverduePenalty(user, loan, now, dailyRate);

        // Assert
        Assert.Equal(1.5m, user.AmountDue); // 3 * 0.5
    }

    [Fact]
    public void ApplyOverduePenalty_does_not_change_AmountDue_when_not_overdue()
    {
        // Arrange
        var user = new UserAccount(Guid.NewGuid(), "Test user", activeLoansCount: 1, amountDue: 10m);
        var borrowedAt = new DateTime(2025, 1, 1);
        var dueDate = new DateTime(2025, 1, 10);
        var now = new DateTime(2025, 1, 10); // pas de retard
        var loan = new Loan(borrowedAt, dueDate);
        var service = new PenaltyService();
        var dailyRate = 0.5m;

        // Act
        service.ApplyOverduePenalty(user, loan, now, dailyRate);

        // Assert
        Assert.Equal(10m, user.AmountDue);
    }
}
