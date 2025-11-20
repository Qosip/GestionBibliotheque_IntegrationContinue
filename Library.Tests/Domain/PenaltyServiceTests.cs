using System;
using Library.Domain.Entities;
using Library.Domain.Services;
using Xunit;

namespace Library.Tests.Domain;

/// <summary>
/// Tests métier du PenaltyService.
/// Les tests couvrent :
/// - le calcul exact des pénalités selon le retard,
/// - les scénarios sans retard,
/// - la cohérence des erreurs,
/// - l’application des montants sur UserAccount,
/// - les cas limites (jour exact, tarif nul, tarif négatif).
/// 
/// Structure AAA + intentions métier explicites.
/// </summary>
public sealed class PenaltyServiceTests
{
    private readonly PenaltyService _service = new();

    // ------------------------------------------------------------
    // UTILS – Construction d’un prêt dans un état cohérent
    // ------------------------------------------------------------

    private static Loan CreateLoan(DateTime borrowedAt, DateTime dueDate)
    {
        return new Loan(
            Guid.NewGuid(),
            Guid.NewGuid(),
            borrowedAt,
            dueDate
        );
    }

    private static UserAccount CreateUserWithBalance(decimal balance = 0m)
    {
        return new UserAccount(Guid.NewGuid(), "User", 0, balance);
    }

    // ------------------------------------------------------------
    // 1. Tests de bord sur les arguments
    // ------------------------------------------------------------

    [Fact]
    public void CalculatePenalty_Should_Throw_When_DailyRate_Negative()
    {
        var loan = CreateLoan(DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddDays(-5));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _service.CalculatePenalty(loan, DateTime.UtcNow, -1m));
    }

    // ------------------------------------------------------------
    // 2. Cas sans retard (pénalité = 0)
    // ------------------------------------------------------------

    [Fact]
    public void CalculatePenalty_Should_Return_Zero_When_Returned_Before_DueDate()
    {
        var borrowed = new DateTime(2025, 1, 10);
        var due = new DateTime(2025, 1, 20);

        var loan = CreateLoan(borrowed, due);

        var now = new DateTime(2025, 1, 18); // Avant la date limite

        var penalty = _service.CalculatePenalty(loan, now, 2m);

        Assert.Equal(0m, penalty);
    }

    [Fact]
    public void CalculatePenalty_Should_Return_Zero_When_Returned_On_DueDate()
    {
        var borrowed = new DateTime(2025, 1, 10);
        var due = new DateTime(2025, 1, 20);

        var loan = CreateLoan(borrowed, due);

        var now = due; // Retour exactement à la limite

        var penalty = _service.CalculatePenalty(loan, now, 3m);

        Assert.Equal(0m, penalty);
    }

    // ------------------------------------------------------------
    // 3. Cas avec retard (pénalité > 0)
    // ------------------------------------------------------------

    [Fact]
    public void CalculatePenalty_Should_Compute_Correct_Amount_For_1_Day_Late()
    {
        var borrowed = new DateTime(2025, 1, 10);
        var due = new DateTime(2025, 1, 20);

        var loan = CreateLoan(borrowed, due);

        var now = new DateTime(2025, 1, 21); // 1 jour de retard

        var penalty = _service.CalculatePenalty(loan, now, 2m);

        Assert.Equal(2m, penalty);
    }

    [Fact]
    public void CalculatePenalty_Should_Compute_Correct_Amount_For_Several_Days()
    {
        var borrowed = new DateTime(2025, 1, 1);
        var due = new DateTime(2025, 1, 10);

        var loan = CreateLoan(borrowed, due);

        var now = new DateTime(2025, 1, 15); // 5 jours de retard

        var penalty = _service.CalculatePenalty(loan, now, 1.5m);

        Assert.Equal(7.5m, penalty);
    }

    // ------------------------------------------------------------
    // 4. Application de la pénalité sur UserAccount
    // ------------------------------------------------------------

    [Fact]
    public void ApplyPenalty_Should_Add_Amount_To_User_When_Late()
    {
        var borrowed = new DateTime(2025, 1, 1);
        var due = new DateTime(2025, 1, 10);

        var loan = CreateLoan(borrowed, due);
        var user = CreateUserWithBalance();

        var now = new DateTime(2025, 1, 12); // 2 jours de retard

        _service.ApplyOverduePenalty(user, loan, now, dailyRate: 10m);

        Assert.Equal(20m, user.AmountDue);
    }

    [Fact]
    public void ApplyPenalty_Should_Not_Add_Anything_If_No_Retard()
    {
        var borrowed = new DateTime(2025, 1, 1);
        var due = new DateTime(2025, 1, 10);

        var loan = CreateLoan(borrowed, due);
        var user = CreateUserWithBalance();

        var now = due; // pas de retard

        _service.ApplyOverduePenalty(user, loan, now, dailyRate: 5m);

        Assert.Equal(0m, user.AmountDue);
    }

    // ------------------------------------------------------------
    // 5. Cas limites – tarif zéro
    // ------------------------------------------------------------

    [Fact]
    public void ApplyPenalty_Should_Do_Nothing_When_DailyRate_Is_Zero()
    {
        var borrowed = new DateTime(2025, 1, 1);
        var due = new DateTime(2025, 1, 10);

        var loan = CreateLoan(borrowed, due);
        var user = CreateUserWithBalance();

        var now = new DateTime(2025, 1, 15); // retard

        _service.ApplyOverduePenalty(user, loan, now, dailyRate: 0m);

        Assert.Equal(0m, user.AmountDue);
    }
}
