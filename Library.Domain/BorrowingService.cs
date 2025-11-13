namespace Library.Domain;

public class BorrowingService
{
    private const int MaxActiveLoans = 5;

    public BorrowResult TryBorrow(UserAccount user)
    {
        if (user.ActiveLoansCount >= MaxActiveLoans)
        {
            return BorrowResult.Fail("BORROW_LIMIT_REACHED");
        }

        user.IncrementActiveLoans();

        return BorrowResult.Ok();
    }
}
