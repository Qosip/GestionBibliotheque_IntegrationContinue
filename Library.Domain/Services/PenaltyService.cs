using System;
using Library.Domain.Entities;

namespace Library.Domain.Services;

public class PenaltyService
{
    public decimal CalculatePenalty(Loan loan, DateTime now, decimal dailyRate)
    {
        if (dailyRate < 0)
            throw new ArgumentOutOfRangeException(nameof(dailyRate), "Daily rate must be non-negative.");

        var overdueDays = loan.GetOverdueDays(now);

        if (overdueDays <= 0)
            return 0m;

        return overdueDays * dailyRate;
    }

    public void ApplyOverduePenalty(UserAccount user, Loan loan, DateTime now, decimal dailyRate)
    {
        var penalty = CalculatePenalty(loan, now, dailyRate);

        if (penalty <= 0m)
            return;

        user.AddAmount(penalty);
    }

}
