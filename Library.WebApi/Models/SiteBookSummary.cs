using System;

namespace Library.WebApi.Models;

public class SiteBookSummary
{
    public Guid BookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
    public int BorrowedCopies { get; set; }
    public int InTransferCopies { get; set; }
}
