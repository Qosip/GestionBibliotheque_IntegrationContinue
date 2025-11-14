using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Library.WebApi.Models;

public class CreateBookWithCopyViewModel
{
    public string Isbn { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public Guid SiteId { get; set; }

    public IEnumerable<SelectListItem> Sites { get; set; } = Array.Empty<SelectListItem>();
}
