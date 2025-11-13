using System;

namespace Library.Domain;

public class UserAccount
{
    public Guid Id { get; }
    public int ActiveLoansCount { get; private set; }
    public decimal AmountDue { get; private set; }

    public UserAccount(Guid id, int activeLoansCount = 0, decimal amountDue = 0m)
    {
        Id = id;
        ActiveLoansCount = activeLoansCount;

        if (amountDue < 0)
            throw new ArgumentOutOfRangeException(nameof(amountDue), "Initial amount due cannot be negative.");

        AmountDue = amountDue;
    }

    public void IncrementActiveLoans()
    {
        ActiveLoansCount++;
    }

    public void DecrementActiveLoans()
    {
        if (ActiveLoansCount == 0)
            throw new InvalidOperationException("Cannot decrement when there are no active loans.");

        ActiveLoansCount--;
    }

    public void AddAmountDue(decimal amount)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be non-negative.");

        AmountDue += amount;
    }
}
