using System;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Library.Domain.Services;
using Xunit;

namespace Library.Tests.Domain;

/// <summary>
/// Suite de tests métier pour BorrowingService.
/// Objectif : valider toutes les règles d’emprunt, y compris limite,
/// disponibilités, cohérence des dates, et cohérence des mutations d’état.
/// 
/// Aucun mock : uniquement du domaine pur.
/// </summary>
public sealed class BorrowingServiceTests
{
    private readonly BorrowingService _service = new();

    // --------------------------------------------------------------------
    // UTILITAIRES POUR GÉNÉRER DES ENTITÉS MÉTIER
    // --------------------------------------------------------------------

    private static UserAccount CreateUser(int activeLoans = 0)
    {
        var u = new UserAccount(Guid.NewGuid(), "Test", 0, 0m);
        for (int i = 0; i < activeLoans; i++)
            u.IncrementLoans(); // Simule des prêts actifs existants
        return u;
    }

    private static BookCopy CreateCopy(BookCopyStatus status = BookCopyStatus.Available)
    {
        var c = new BookCopy(Guid.NewGuid(), Guid.NewGuid());
        if (status == BookCopyStatus.Borrowed)
            c.MarkAsBorrowed();
        if (status == BookCopyStatus.InTransfer)
            c.MarkAsInTransfer();
        return c;
    }

    // --------------------------------------------------------------------
    // 1) Tests limites emprunts
    // --------------------------------------------------------------------

    [Fact]
    public void TryBorrow_Should_Fail_When_User_Reaches_Max_Loans()
    {
        // Intention :
        // L'utilisateur possède déjà le maximum de prêts autorisés.
        // => L'emprunt doit être refusé avec le code BORROW_LIMIT_REACHED.

        var user = CreateUser(activeLoans: 5);
        var copy = CreateCopy();

        var now = DateTime.UtcNow;
        var result = _service.TryBorrow(user, copy, now, now.AddDays(14));

        Assert.False(result.Success);
        Assert.Equal("BORROW_LIMIT_REACHED", result.ErrorCode);
    }

    [Fact]
    public void TryBorrow_Should_Succeed_When_User_Has_4_Loans()
    {
        var user = CreateUser(activeLoans: 4);
        var copy = CreateCopy();

        var now = DateTime.UtcNow;
        var result = _service.TryBorrow(user, copy, now, now.AddDays(14));

        Assert.True(result.Success);
        Assert.NotNull(result.Loan);
        Assert.Equal(5, user.ActiveLoansCount); // incrément correct
        Assert.Equal(BookCopyStatus.Borrowed, copy.Status);
    }

    // --------------------------------------------------------------------
    // 2) Tests disponibilité exemplaires
    // --------------------------------------------------------------------

    [Fact]
    public void TryBorrow_Should_Fail_When_Copy_Not_Available()
    {
        var user = CreateUser();
        var copy = CreateCopy(status: BookCopyStatus.Borrowed);

        var now = DateTime.UtcNow;

        var result = _service.TryBorrow(user, copy, now, now.AddDays(14));

        Assert.False(result.Success);
        Assert.Equal("COPY_NOT_AVAILABLE", result.ErrorCode);
    }

    [Fact]
    public void TryBorrow_Should_Fail_When_Copy_In_Transfer()
    {
        var user = CreateUser();
        var copy = CreateCopy(status: BookCopyStatus.InTransfer);

        var now = DateTime.UtcNow;

        var result = _service.TryBorrow(user, copy, now, now.AddDays(14));

        Assert.False(result.Success);
        Assert.Equal("COPY_NOT_AVAILABLE", result.ErrorCode);
    }

    // --------------------------------------------------------------------
    // 3) Tests cohérence des dates
    // --------------------------------------------------------------------

    [Fact]
    public void TryBorrow_Should_Throw_When_DueDate_Before_BorrowedAt()
    {
        var user = CreateUser();
        var copy = CreateCopy();

        var borrowedAt = DateTime.UtcNow;
        var dueDate = borrowedAt.AddHours(-1);

        Assert.Throws<ArgumentException>(() =>
            _service.TryBorrow(user, copy, borrowedAt, dueDate));
    }

    // --------------------------------------------------------------------
    // 4) Tests mutation d’état cohérente
    // --------------------------------------------------------------------

    [Fact]
    public void TryBorrow_Should_Mark_Copy_As_Borrowed_On_Success()
    {
        var user = CreateUser();
        var copy = CreateCopy();

        var now = DateTime.UtcNow;
        var result = _service.TryBorrow(user, copy, now, now.AddDays(14));

        Assert.True(result.Success);
        Assert.Equal(BookCopyStatus.Borrowed, copy.Status);
    }

    [Fact]
    public void TryBorrow_Should_Increment_User_ActiveLoans_On_Success()
    {
        var user = CreateUser(activeLoans: 1);
        var copy = CreateCopy();

        var now = DateTime.UtcNow;
        var result = _service.TryBorrow(user, copy, now, now.AddDays(14));

        Assert.True(result.Success);
        Assert.Equal(2, user.ActiveLoansCount);
    }

    // --------------------------------------------------------------------
    // 5) Tests cohérence du Loan retourné
    // --------------------------------------------------------------------

    [Fact]
    public void TryBorrow_Should_Return_Loan_With_Correct_Data()
    {
        var user = CreateUser();
        var copy = CreateCopy();

        var borrowedAt = new DateTime(2025, 01, 20, 10, 00, 00, DateTimeKind.Utc);
        var dueDate = borrowedAt.AddDays(14);

        var result = _service.TryBorrow(user, copy, borrowedAt, dueDate);

        Assert.True(result.Success);
        Assert.NotNull(result.Loan);

        var loan = result.Loan!;

        Assert.Equal(user.Id, loan.UserAccountId);
        Assert.Equal(copy.Id, loan.BookCopyId);
        Assert.Equal(borrowedAt, loan.BorrowedAt);
        Assert.Equal(dueDate, loan.DueDate);
        Assert.Null(loan.ReturnedAt);
    }

    // --------------------------------------------------------------------
    // 6) Tests erreurs argumentaires
    // --------------------------------------------------------------------

    [Fact]
    public void TryBorrow_Should_Throw_If_User_Null()
    {
        var copy = CreateCopy();
        var now = DateTime.UtcNow;

        Assert.Throws<ArgumentNullException>(() =>
            _service.TryBorrow(null!, copy, now, now.AddDays(14)));
    }

    [Fact]
    public void TryBorrow_Should_Throw_If_Copy_Null()
    {
        var user = CreateUser();
        var now = DateTime.UtcNow;

        Assert.Throws<ArgumentNullException>(() =>
            _service.TryBorrow(user, null!, now, now.AddDays(14)));
    }
}
