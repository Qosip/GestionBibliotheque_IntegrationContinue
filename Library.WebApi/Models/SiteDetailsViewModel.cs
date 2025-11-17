using System;
using System.Collections.Generic;

namespace Library.WebApi.Models;

public class SiteDetailsViewModel
{
    public Guid SiteId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }

    public List<SiteBookSummary> Books { get; set; } = new();
}
