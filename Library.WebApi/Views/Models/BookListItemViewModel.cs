using System;

namespace Library.WebApi.Views.Models;

public sealed class BookListItemViewModel
{
    public Guid Id { get; set; }
    public string Isbn { get; set; } = "";
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
}
