using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Library.WebApi.Models;

public class TransferRequestViewModel
{
    public Guid BookId { get; set; }
    public Guid SourceSiteId { get; set; }
    public Guid TargetSiteId { get; set; }

    public int Quantity { get; set; } = 1;

    public IEnumerable<SelectListItem> Books { get; set; } = Array.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Sites { get; set; } = Array.Empty<SelectListItem>();
}
