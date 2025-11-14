using System;
using System.Collections.Generic;

namespace Library.WebApi.Models;

public class UserDetailsViewModel
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ActiveLoansCount { get; set; }
    public decimal AmountDue { get; set; }

    public List<UserLoanInfo> Loans { get; set; } = new();
}
