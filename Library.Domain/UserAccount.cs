using System;

namespace Library.Domain;

public class UserAccount
{
    public Guid Id { get; }
    public int ActiveLoansCount { get; private set; }

    public UserAccount(Guid id, int activeLoansCount = 0)
    {
        Id = id;
        ActiveLoansCount = activeLoansCount;
    }

    public void IncrementActiveLoans()
    {
        ActiveLoansCount++;
    }
}
