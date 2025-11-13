using System;

namespace Library.Domain;

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
}
