using System;
using Library.Domain;

namespace Library.Application;

public sealed class ReturnBookHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IBookCopyRepository _bookCopyRepository;
    private readonly ILoanRepository _loanRepository;
    private readonly IClock _clock;
    private readonly ReturnService _returnService;

    // cohérent avec nos tests : 0.5 par jour de retard
    private const decimal DefaultDailyRate = 0.5m;

    public ReturnBookHandler(
        IUserRepository userRepository,
        IBookCopyRepository bookCopyRepository,
        ILoanRepository loanRepository,
        IClock clock,
        ReturnService returnService)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _bookCopyRepository = bookCopyRepository ?? throw new ArgumentNullException(nameof(bookCopyRepository));
        _loanRepository = loanRepository ?? throw new ArgumentNullException(nameof(loanRepository));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _returnService = returnService ?? throw new ArgumentNullException(nameof(returnService));
    }

    public ReturnBookResult Handle(ReturnBookCommand command)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));

        var loan = _loanRepository.GetById(command.LoanId);
        if (loan is null)
        {
            return ReturnBookResult.Fail("LOAN_NOT_FOUND");
        }

        var user = _userRepository.GetById(loan.UserAccountId);
        if (user is null)
        {
            return ReturnBookResult.Fail("USER_NOT_FOUND");
        }

        var copy = _bookCopyRepository.GetById(loan.BookCopyId);
        if (copy is null)
        {
            return ReturnBookResult.Fail("COPY_NOT_FOUND");
        }

        var returnDate = _clock.UtcNow;

        // Domain: pénalités + compteur + ReturnedAt
        _returnService.ReturnBook(user, loan, returnDate, DefaultDailyRate);

        // Domain ne connaît pas BookCopy : on le remet dispo ici
        copy.MarkAsReturned();

        return ReturnBookResult.Ok();
    }
}
