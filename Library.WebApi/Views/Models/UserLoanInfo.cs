using System;

namespace Library.WebApi.Models;

public class UserLoanInfo
{
    public Guid LoanId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string SiteName { get; set; } = string.Empty;
    public DateTime BorrowedAt { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ReturnedAt { get; set; }

    public string Status => ReturnedAt.HasValue ? "Retourné" : "En cours";
}
