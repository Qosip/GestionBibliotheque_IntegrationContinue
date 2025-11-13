using System;
using System.Collections.Generic;
using Library.Domain;
using Xunit;

namespace Library.Tests;

public class PenaltyCalculationTests
{
    public static IEnumerable<object[]> PenaltyCases =>
        new List<object[]>
        {
            // borrowedAt,        dueDate,           now,               dailyRate, expectedPenalty
            new object[] { new DateTime(2025, 1, 1), new DateTime(2025, 1, 10), new DateTime(2025, 1, 9),  0.5m, 0m },    // avant due date
            new object[] { new DateTime(2025, 1, 1), new DateTime(2025, 1, 10), new DateTime(2025, 1, 10), 0.5m, 0m },    // exactement due date
            new object[] { new DateTime(2025, 1, 1), new DateTime(2025, 1, 10), new DateTime(2025, 1, 11), 0.5m, 0.5m },  // 1 jour de retard
            new object[] { new DateTime(2025, 1, 1), new DateTime(2025, 1, 10), new DateTime(2025, 1, 15), 0.5m, 2.5m },  // 5 jours de retard
            new object[] { new DateTime(2025, 1, 1), new DateTime(2025, 1, 10), new DateTime(2025, 2, 10), 1.0m, 31m },   // retard massif
            new object[] { new DateTime(2025, 1, 1), new DateTime(2025, 1, 10), new DateTime(2025, 1, 15), 0m,   0m },    // taux nul
        };

    [Theory]
    [MemberData(nameof(PenaltyCases))]
    public void CalculatePenalty_returns_expected_amount(
        DateTime borrowedAt,
        DateTime dueDate,
        DateTime now,
        decimal dailyRate,
        decimal expectedPenalty)
    {
        // Arrange
        var loan = new Loan(borrowedAt, dueDate);
        var service = new PenaltyService();

        // Act
        var penalty = service.CalculatePenalty(loan, now, dailyRate);

        // Assert
        Assert.Equal(expectedPenalty, penalty);
    }

    [Fact]
    public void CalculatePenalty_throws_when_dailyRate_is_negative()
    {
        // Arrange
        var borrowedAt = new DateTime(2025, 1, 1);
        var dueDate = new DateTime(2025, 1, 10);
        var now = new DateTime(2025, 1, 15);
        var loan = new Loan(borrowedAt, dueDate);
        var service = new PenaltyService();

        // Act + Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            service.CalculatePenalty(loan, now, -0.5m);
        });
    }
}
