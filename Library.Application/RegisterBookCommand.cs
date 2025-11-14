using System;

namespace Library.Application;

public sealed class RegisterBookCommand
{
    public string Isbn { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;

    public RegisterBookCommand()
    {
    }

    public RegisterBookCommand(string isbn, string title, string author)
    {
        Isbn = isbn;
        Title = title;
        Author = author;
    }
}
