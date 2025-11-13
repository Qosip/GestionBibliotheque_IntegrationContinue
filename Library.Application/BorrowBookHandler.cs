using System;
using Library.Domain;

namespace Library.Application;

public sealed class BorrowBookHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IBookCopyRepository _bookCopyRepository;
    private readonly ILoanRepository _loanRepository;
    private readonly IClock _clock;
    private readonly BorrowingService _borrowingService;

    private const int DefaultLoanDurationDays = 14;

    public BorrowBookHandler(
        IUserRepository userRepository,
        IBookCopyRepository bookCopyRepository,
        ILoanRepository loanRepository,
        IClock clock,
        BorrowingService borrowingService)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _bookCopyRepository = bookCopyRepository ?? throw new ArgumentNullException(nameof(bookCopyRepository));
        _loanRepository = loanRepository ?? throw new ArgumentNullException(nameof(loanRepository));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _borrowingService = borrowingService ?? throw new ArgumentNullException(nameof(borrowingService));
    }

    public BorrowBookResult Handle(BorrowBookCommand command)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));

        var user = _userRepository.GetById(command.UserId);
        if (user is null)
        {
            return BorrowBookResult.Fail("USER_NOT_FOUND");
        }

        var copy = _bookCopyRepository.FindAvailableCopy(command.BookId, command.SiteId);
        if (copy is null)
        {
            return BorrowBookResult.Fail("NO_COPY_AVAILABLE_AT_SITE");
        }

        var borrowedAt = _clock.UtcNow;
        var dueDate = borrowedAt.AddDays(DefaultLoanDurationDays);

        var borrowResult = _borrowingService.TryBorrow(user, copy, borrowedAt, dueDate);

        if (!borrowResult.Success || borrowResult.Loan is null)
        {
            // On propage l’erreur métier telle quelle
            return BorrowBookResult.Fail(borrowResult.ErrorCode ?? "BORROW_FAILED");
        }

        _loanRepository.Add(borrowResult.Loan);

        return BorrowBookResult.Ok(borrowResult.Loan.Id);
    }
}
