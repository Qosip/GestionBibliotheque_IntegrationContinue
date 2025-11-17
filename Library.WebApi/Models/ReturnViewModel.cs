using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Library.WebApi.Models;

public class ReturnViewModel
{
    public Guid UserId { get; set; }
    public Guid LoanId { get; set; }

    public IEnumerable<SelectListItem> Users { get; set; } = Array.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Loans { get; set; } = Array.Empty<SelectListItem>();
}
