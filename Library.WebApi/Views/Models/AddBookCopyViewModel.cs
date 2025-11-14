using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Library.WebApi.Views.Models;

public sealed class AddBookCopyViewModel
{
    [Required]
    [Display(Name = "Livre")]
    public Guid SelectedBookId { get; set; }

    [Required]
    [Display(Name = "Site")]
    public Guid SelectedSiteId { get; set; }

    [Required]
    [Range(1, 100, ErrorMessage = "La quantité doit être entre 1 et 100.")]
    [Display(Name = "Quantité d'exemplaires")]
    public int Quantity { get; set; } = 1;

    public List<SelectListItem> Books { get; set; } = new();
    public List<SelectListItem> Sites { get; set; } = new();
}
