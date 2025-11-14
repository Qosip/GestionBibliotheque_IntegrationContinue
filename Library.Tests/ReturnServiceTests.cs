using System;
using Library.Domain;
using Xunit;

namespace Library.Tests;

public class ReturnServiceTests
{
    [Theory]
    // borrowedAt      dueDate         returnDate      dailyRate  initialAmount  expectedAmount
    [InlineData(2025, 1, 1, 2025, 1, 10, 2025, 1, 10, 0.5, 0.0, 0.0)]  // retour à la date limite → pas de pénalité
    [InlineData(2025, 1, 1, 2025, 1, 10, 2025, 1, 9, 0.5, 2.0, 2.0)]  // retour avant la date limite → pas de pénalité
    [InlineData(2025, 1, 1, 2025, 1, 10, 2025, 1, 11, 0.5, 0.0, 0.5)]  // 1 jour de retard → 0.5
    [InlineData(2025, 1, 1, 2025, 1, 10, 2025, 1, 13, 0.5, 1.0, 2.5)]  // 3 jours de retard → 1.0 + 1.5
    public void ReturnBook_updates_loan_and_user_and_applies_penalty_if_needed(
        int bYear, int bMonth, int bDay,
        int dYear, int dMonth, int dDay,
        int rYear, int rMonth, int rDay,
        decimal dailyRate,
        decimal initialAmountDue,
        decimal expectedAmountDue)
    {
        // Arrange
        var borrowedAt = new DateTime(bYear, bMonth, bDay);
        var dueDate = new DateTime(dYear, dMonth, dDay);
        var returnDate = new DateTime(rYear, rMonth, rDay);

        var user = new UserAccount(Guid.NewGuid(), "Test user", activeLoansCount: 1, amountDue: initialAmountDue);
        var loan = new Loan(borrowedAt, dueDate);

        var penaltyService = new PenaltyService();
        var returnService = new ReturnService(penaltyService);

        // Act
        returnService.ReturnBook(user, loan, returnDate, dailyRate);

        // Assert
        Assert.Equal(expectedAmountDue, user.AmountDue);          // somme due mise à jour
        Assert.Equal(returnDate, loan.ReturnedAt);                // prêt marqué comme retourné
        Assert.Equal(0, user.ActiveLoansCount);                   // compteur décrémenté
    }

    [Fact]
    public void ReturnBook_throws_when_loan_is_already_returned()
    {
        // Arrange
        var borrowedAt = new DateTime(2025, 1, 1);
        var dueDate = new DateTime(2025, 1, 10);
        var firstReturn = new DateTime(2025, 1, 11);
        var secondReturn = new DateTime(2025, 1, 12);

        var user = new UserAccount(Guid.NewGuid(), "Test user", activeLoansCount: 1, amountDue: 0m);
        var loan = new Loan(borrowedAt, dueDate);
        loan.MarkAsReturned(firstReturn);

        var penaltyService = new PenaltyService();
        var returnService = new ReturnService(penaltyService);

        // Act + Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            returnService.ReturnBook(user, loan, secondReturn, dailyRate: 0.5m);
        });

        // On s’assure que le compte n’a pas été touché
        Assert.Equal(1, user.ActiveLoansCount);
        Assert.Equal(0m, user.AmountDue);
        Assert.Equal(firstReturn, loan.ReturnedAt);
    }
}
