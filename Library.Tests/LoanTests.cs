using System;
using Library.Domain;
using Library.Domain.Entities;
using Xunit;

namespace Library.Tests.Domain;

/// <summary>
/// Suite de tests métier pour Loan.
/// Couvre :
/// - invariants de construction,
/// - logique de retard (IsOverdue / GetOverdueDays),
/// - comportement selon date de retour,
/// - cas limites.
/// </summary>
public sealed class LoanTests
{
    // ------------------------------------------------------------
    // 1) Constructeurs & invariants
    // ------------------------------------------------------------

    [Fact]
    public void Ctor_Should_Throw_If_DueDate_Before_BorrowedAt()
    {
        var borrowed = new DateTime(2025, 1, 10);
        var due = borrowed.AddDays(-1); // incohérent

        Assert.Throws<ArgumentException>(() =>
            new Loan(borrowed, due));
    }

    [Fact]
    public void FullCtor_Should_Set_All_Ids()
    {
        var userId = Guid.NewGuid();
        var copyId = Guid.NewGuid();

        var borrowed = new DateTime(2025, 1, 10);
        var due = borrowed.AddDays(7);

        var loan = new Loan(userId, copyId, borrowed, due);

        Assert.Equal(userId, loan.UserAccountId);
        Assert.Equal(copyId, loan.BookCopyId);
        Assert.Equal(borrowed, loan.BorrowedAt);
        Assert.Equal(due, loan.DueDate);
        Assert.NotEqual(Guid.Empty, loan.Id);
    }

    // ------------------------------------------------------------
    // 2) IsOverdue(now) : logique du retard
    // ------------------------------------------------------------

    [Fact]
    public void IsOverdue_Should_Throw_When_Now_Before_BorrowedAt()
    {
        var borrowed = new DateTime(2025, 1, 10);
        var due = borrowed.AddDays(10);

        var loan = new Loan(borrowed, due);

        var now = borrowed.AddDays(-1);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            loan.IsOverdue(now));
    }

    [Fact]
    public void IsOverdue_Should_Return_False_When_Now_Before_DueDate()
    {
        var loan = new Loan(
            borrowedAt: new DateTime(2025, 1, 1),
            dueDate: new DateTime(2025, 1, 10));

        var now = new DateTime(2025, 1, 5);

        Assert.False(loan.IsOverdue(now));
    }

    [Fact]
    public void IsOverdue_Should_Return_False_When_Now_On_DueDate()
    {
        var loan = new Loan(
            borrowedAt: new DateTime(2025, 1, 1),
            dueDate: new DateTime(2025, 1, 10));

        var now = new DateTime(2025, 1, 10);

        Assert.False(loan.IsOverdue(now));
    }

    [Fact]
    public void IsOverdue_Should_Return_True_When_Now_After_DueDate()
    {
        var loan = new Loan(
            new DateTime(2025, 1, 1),
            new DateTime(2025, 1, 10));

        var now = new DateTime(2025, 1, 11);

        Assert.True(loan.IsOverdue(now));
    }

    // ------------------------------------------------------------
    // 3) GetOverdueDays(now) : calcul du nombre de jours de retard
    // ------------------------------------------------------------

    [Fact]
    public void GetOverdueDays_Should_Throw_When_Now_Before_BorrowedAt()
    {
        var borrowed = new DateTime(2025, 1, 10);
        var due = borrowed.AddDays(5);

        var loan = new Loan(borrowed, due);

        var now = borrowed.AddDays(-1);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            loan.GetOverdueDays(now));
    }

    [Fact]
    public void GetOverdueDays_Should_Return_0_When_Not_Overdue()
    {
        var loan = new Loan(
            borrowedAt: new DateTime(2025, 1, 1),
            dueDate: new DateTime(2025, 1, 10));

        Assert.Equal(0, loan.GetOverdueDays(new DateTime(2025, 1, 1)));
        Assert.Equal(0, loan.GetOverdueDays(new DateTime(2025, 1, 10)));
    }

    [Fact]
    public void GetOverdueDays_Should_Return_Number_Of_OverdueDays()
    {
        var loan = new Loan(
            borrowedAt: new DateTime(2025, 1, 1),
            dueDate: new DateTime(2025, 1, 10));

        var now = new DateTime(2025, 1, 15);

        Assert.Equal(5, loan.GetOverdueDays(now));
    }

    // ------------------------------------------------------------
    // 4) Effet de ReturnedAt sur la logique
    // ------------------------------------------------------------
    
    [Fact]
    public void IsOverdue_Should_Use_ReturnedAt_When_Late()
    {
        var borrowed = new DateTime(2025, 1, 1);
        var due = new DateTime(2025, 1, 10);

        var loan = new Loan(borrowed, due);

        var returnedAt = new DateTime(2025, 1, 12); // 2 jours de retard
        loan.MarkAsReturned(returnedAt);

        Assert.True(loan.IsOverdue(returnedAt));
    }
    
    [Fact]
    public void GetOverdueDays_Should_Use_ReturnedAt_When_Set()
    {
        var loan = new Loan(
            new DateTime(2025, 1, 1),
            new DateTime(2025, 1, 10));

        loan.MarkAsReturned(new DateTime(2025, 1, 15)); // 5 jours en retard

        Assert.Equal(5, loan.GetOverdueDays(new DateTime(2025, 1, 20)));
        // même si le now est plus tard, le calcul s’appuie sur ReturnedAt
    }

    // ------------------------------------------------------------
    // 5) MarkAsReturned invariants
    // ------------------------------------------------------------

    [Fact]
    public void MarkAsReturned_Should_Throw_If_ReturnDate_Before_BorrowedAt()
    {
        var borrowed = new DateTime(2025, 1, 10);
        var due = borrowed.AddDays(2);

        var loan = new Loan(borrowed, due);

        var returnDate = borrowed.AddDays(-1);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            loan.MarkAsReturned(returnDate));
    }

    [Fact]
    public void MarkAsReturned_Should_Set_ReturnedAt()
    {
        var borrowed = new DateTime(2025, 1, 1);
        var due = borrowed.AddDays(10);

        var loan = new Loan(borrowed, due);

        var returnDate = new DateTime(2025, 1, 12);

        loan.MarkAsReturned(returnDate);

        Assert.Equal(returnDate, loan.ReturnedAt);
    }
}
