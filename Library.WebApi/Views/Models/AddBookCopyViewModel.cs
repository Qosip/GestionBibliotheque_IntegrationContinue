using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Library.WebApi.Models;

public class AddBookCopyViewModel
{
    public Guid BookId { get; set; }
    public Guid SiteId { get; set; }

    public IEnumerable<SelectListItem> Books { get; set; } = Array.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Sites { get; set; } = Array.Empty<SelectListItem>();
}
