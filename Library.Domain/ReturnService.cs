using System;

namespace Library.Domain;

public class ReturnService
{
    private readonly PenaltyService _penaltyService;

    public ReturnService(PenaltyService penaltyService)
    {
        _penaltyService = penaltyService ?? throw new ArgumentNullException(nameof(penaltyService));
    }

    public void ReturnBook(UserAccount user, Loan loan, DateTime returnDate, decimal dailyRate)
    {
        if (user is null) throw new ArgumentNullException(nameof(user));
        if (loan is null) throw new ArgumentNullException(nameof(loan));

        if (loan.ReturnedAt.HasValue)
            throw new InvalidOperationException("Loan is already returned.");

        // Marque le prêt comme retourné (valide la date)
        loan.MarkAsReturned(returnDate);

        // Applique la pénalité éventuelle (0 si pas de retard)
        _penaltyService.ApplyOverduePenalty(user, loan, returnDate, dailyRate);

        // Décrémente le compteur d’emprunts actifs
        user.DecrementLoans();
    }
}
