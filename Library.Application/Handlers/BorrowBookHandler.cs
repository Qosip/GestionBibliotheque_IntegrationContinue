using System;
using Library.Application.Commands;
using Library.Application.Repositories;
using Library.Application.Results;
using Library.Domain.Services;

namespace Library.Application.Handlers;

public class BorrowBookHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IBookCopyRepository _bookCopyRepository;
    private readonly ILoanRepository _loanRepository;
    private readonly IClock _clock;
    private readonly BorrowingService _borrowingService;

    public BorrowBookHandler(
        IUserRepository userRepository,
        IBookCopyRepository bookCopyRepository,
        ILoanRepository loanRepository,
        IClock clock,
        BorrowingService borrowingService)
    {
        _userRepository = userRepository;
        _bookCopyRepository = bookCopyRepository;
        _loanRepository = loanRepository;
        _clock = clock;
        _borrowingService = borrowingService;
    }

    public BorrowBookResult Handle(BorrowBookCommand command)
    {
        if (command is null)
            throw new ArgumentNullException(nameof(command));

        var user = _userRepository.GetById(command.UserId);
        if (user is null)
            return BorrowBookResult.Fail("USER_NOT_FOUND");

        var copy = _bookCopyRepository.FindAvailableCopy(command.BookId, command.SiteId);
        if (copy is null)
            return BorrowBookResult.Fail("NO_COPY_AVAILABLE_AT_SITE");

        var borrowedAt = _clock.UtcNow;
        var dueDate = borrowedAt.AddDays(14);

        var borrowResult = _borrowingService.TryBorrow(user, copy, borrowedAt, dueDate);
        if (!borrowResult.Success)
            return BorrowBookResult.Fail(borrowResult.ErrorCode ?? "BORROW_FAILED");

        var loan = borrowResult.Loan!;

        // Persistance via repositories (chacun fait SaveChanges)
        _bookCopyRepository.Update(copy);
        _userRepository.Update(user);
        _loanRepository.Add(loan);

        return BorrowBookResult.Ok(loan.Id);
    }
}