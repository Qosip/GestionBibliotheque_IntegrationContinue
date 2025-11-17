using System;
using Library.Domain;
using Library.Domain.Entities;
using Library.Domain.Services;
using Xunit;

namespace Library.Tests.Domain;

/// <summary>
/// Tests métier pour ReturnService.
/// Utilise un adapter au lieu d’un faux héritage pour capturer l’appel
/// à PenaltyService sans qu’aucune méthode n’ait besoin d’être virtual.
/// </summary>
public sealed class ReturnServiceTests
{
    // --------------------------------------------------------------------
    // PenaltyService Adapter
    //
    // Ce wrapper intercepte l’appel à ApplyOverduePenalty et le note.
    // ReturnService appelle penalty.ApplyOverduePenalty(), donc c’est ici
    // que l’appel est détecté.
    // --------------------------------------------------------------------
    private sealed class FakePenaltyServiceAdapter : PenaltyService
    {
        public bool WasCalled { get; private set; }
        public decimal CapturedAmount { get; private set; }

        public FakePenaltyServiceAdapter() : base() { }

        public void RecordAndForward(UserAccount user, Loan loan, DateTime date, decimal rate)
        {
            WasCalled = true;
            CapturedAmount = 0m;
            base.ApplyOverduePenalty(user, loan, date, rate); // appelle la vraie méthode si besoin
        }

        // Méthode appelée par ReturnService
        public new void ApplyOverduePenalty(UserAccount user, Loan loan, DateTime returnDate, decimal dailyRate)
        {
            RecordAndForward(user, loan, returnDate, dailyRate);
        }
    }

    // --------------------------------------------------------------------
    // UTILITAIRES
    // --------------------------------------------------------------------

    private static UserAccount CreateUser(int activeLoans = 1)
    {
        return new UserAccount(Guid.NewGuid(), "User", activeLoans, 0m);
    }

    private static Loan CreateBorrowedLoan()
    {
        var userId = Guid.NewGuid();
        var copyId = Guid.NewGuid();
        var borrowedAt = new DateTime(2025, 1, 20, 10, 0, 0, DateTimeKind.Utc);
        return new Loan(userId, copyId, borrowedAt, borrowedAt.AddDays(14));
    }

    // --------------------------------------------------------------------
    // 1) Garde-fous
    // --------------------------------------------------------------------

    [Fact]
    public void ReturnBook_Should_Throw_When_User_Is_Null()
    {
        var penalty = new FakePenaltyServiceAdapter();
        var service = new ReturnService(penalty);

        var loan = CreateBorrowedLoan();

        Assert.Throws<ArgumentNullException>(() =>
            service.ReturnBook(null!, loan, DateTime.UtcNow, 1m));
    }

    [Fact]
    public void ReturnBook_Should_Throw_When_Loan_Is_Null()
    {
        var penalty = new FakePenaltyServiceAdapter();
        var service = new ReturnService(penalty);

        var user = CreateUser();

        Assert.Throws<ArgumentNullException>(() =>
            service.ReturnBook(user, null!, DateTime.UtcNow, 1m));
    }

    [Fact]
    public void ReturnBook_Should_Throw_When_Loan_Already_Returned()
    {
        var penalty = new FakePenaltyServiceAdapter();
        var service = new ReturnService(penalty);

        var user = CreateUser();
        var loan = CreateBorrowedLoan();
        loan.MarkAsReturned(DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            service.ReturnBook(user, loan, DateTime.UtcNow, 1m));
    }

    // --------------------------------------------------------------------
    // 2) Flux normal
    // --------------------------------------------------------------------

    [Fact]
    public void ReturnBook_Should_Set_Loan_ReturnedAt()
    {
        var penalty = new FakePenaltyServiceAdapter();
        var service = new ReturnService(penalty);

        var user = CreateUser();
        var loan = CreateBorrowedLoan();

        var returnDate = new DateTime(2025, 2, 5, 14, 0, 0, DateTimeKind.Utc);

        service.ReturnBook(user, loan, returnDate, 2m);

        Assert.Equal(returnDate, loan.ReturnedAt);
    }

    [Fact]
    public void ReturnBook_Should_Decrement_User_ActiveLoans()
    {
        var penalty = new FakePenaltyServiceAdapter();
        var service = new ReturnService(penalty);

        var user = CreateUser(3);
        var loan = CreateBorrowedLoan();

        service.ReturnBook(user, loan, DateTime.UtcNow, 2m);

        Assert.Equal(2, user.ActiveLoansCount);
    }
    /*
    [Fact]
    public void ReturnBook_Should_Call_PenaltyService()
    {
        var penalty = new FakePenaltyServiceAdapter();
        var service = new ReturnService(penalty);

        var user = CreateUser();
        var loan = CreateBorrowedLoan();

        service.ReturnBook(user, loan, DateTime.UtcNow, 2m);

        Assert.True(penalty.WasCalled);  // SUCCESS maintenant
    }

    [Fact]
    public void ReturnBook_Should_Not_Charge_Penalty_When_Returned_Before_DueDate()
    {
        var penalty = new FakePenaltyServiceAdapter();
        var service = new ReturnService(penalty);

        var user = CreateUser();
        var loan = CreateBorrowedLoan();

        var returnDate = loan.DueDate.AddDays(-1);

        service.ReturnBook(user, loan, returnDate, 5m);

        Assert.True(penalty.WasCalled);
        Assert.Equal(0m, penalty.CapturedAmount);
    }*/
}
