using System;
using Library.Domain;
using Library.Domain.Entities;
using Xunit;

namespace Library.Tests.Domain;

/// <summary>
/// Tests métier pour UserAccount.
/// Valide :
/// - les invariants (nom obligatoire),
/// - la gestion des emprunts actifs,
/// - la gestion des montants dus,
/// - le trimming,
/// - l’intégrité de l’identité.
/// </summary>
public sealed class UserAccountTests
{
    // ------------------------------------------------------------
    // 1) Invariants – nom obligatoire
    // ------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_Should_Throw_When_Name_Invalid(string? name)
    {
        Assert.Throws<ArgumentException>(() =>
            new UserAccount(Guid.NewGuid(), name!));
    }

    // ------------------------------------------------------------
    // 2) Trimming automatique
    // ------------------------------------------------------------

    [Fact]
    public void Ctor_Should_Trim_Name()
    {
        var user = new UserAccount(Guid.NewGuid(), "  Alice   ");

        Assert.Equal("Alice", user.Name);
    }

    // ------------------------------------------------------------
    // 3) Identité (Id stable)
    // ------------------------------------------------------------

    [Fact]
    public void Ctor_Should_Set_Id_Exactly_As_Provided()
    {
        var id = Guid.NewGuid();

        var user = new UserAccount(id, "Bob");

        Assert.Equal(id, user.Id);
    }

    // ------------------------------------------------------------
    // 4) Gestion des emprunts actifs
    // ------------------------------------------------------------

    [Fact]
    public void IncrementLoans_Should_Increase_ActiveLoansCount()
    {
        var user = new UserAccount(Guid.NewGuid(), "User", 1, 0m);

        user.IncrementLoans();

        Assert.Equal(2, user.ActiveLoansCount);
    }

    [Fact]
    public void DecrementLoans_Should_Decrease_ActiveLoansCount()
    {
        var user = new UserAccount(Guid.NewGuid(), "User", 3, 0m);

        user.DecrementLoans();

        Assert.Equal(2, user.ActiveLoansCount);
    }

    // ------------------------------------------------------------
    // 5) Gestion des montants dus
    // ------------------------------------------------------------

    [Fact]
    public void AddAmount_Should_Increase_AmountDue()
    {
        var user = new UserAccount(Guid.NewGuid(), "User", 0, 10m);

        user.AddAmount(5m);

        Assert.Equal(15m, user.AmountDue);
    }

    [Fact]
    public void PayAmount_Should_Decrease_AmountDue()
    {
        var user = new UserAccount(Guid.NewGuid(), "User", 0, 20m);

        user.PayAmount(7m);

        Assert.Equal(13m, user.AmountDue);
    }

    [Fact]
    public void PayAmount_Should_Allow_Overpaying_BelowZero_Because_Domain_Does_Not_Block_It()
    {
        // Intention métier :
        // La couche domaine actuelle n'interdit pas de payer plus que ce qui est dû.
        // On valide donc explicitement ce comportement pour éviter tout changement
        // silencieux dans le futur.

        var user = new UserAccount(Guid.NewGuid(), "User", 0, 5m);

        user.PayAmount(10m);

        Assert.Equal(-5m, user.AmountDue);
    }

    [Fact]
    public void AddAmount_Should_Support_Cumulative_Charges()
    {
        var user = new UserAccount(Guid.NewGuid(), "User", 0, 0m);

        user.AddAmount(3m);
        user.AddAmount(2m);
        user.AddAmount(10m);

        Assert.Equal(15m, user.AmountDue);
    }

    // ------------------------------------------------------------
    // 6) Cas avancé : modification complète via constructeur complet
    // ------------------------------------------------------------

    [Fact]
    public void FullCtor_Should_Set_All_Properties()
    {
        var id = Guid.NewGuid();

        var user = new UserAccount(id, "Charlie", 3, 12.5m);

        Assert.Equal(id, user.Id);
        Assert.Equal("Charlie", user.Name);
        Assert.Equal(3, user.ActiveLoansCount);
        Assert.Equal(12.5m, user.AmountDue);
    }
}
