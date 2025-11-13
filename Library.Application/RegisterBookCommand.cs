using System;

namespace Library.Application;

public sealed class RegisterBookCommand
{
    public string Isbn { get; }
    public string Title { get; }
    public string Author { get; }

    public RegisterBookCommand(string isbn, string title, string author)
    {
        Isbn = isbn;
        Title = title;
        Author = author;
    }
}
