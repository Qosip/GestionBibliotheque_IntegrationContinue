using System;

namespace Library.Domain.Entities;

public class Book
{
    public Guid Id { get; private set; }
    public string Isbn { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Author { get; private set; } = string.Empty;

    private Book()
    {
    }

    public Book(Guid id, string isbn, string title, string author)
    {
        if (string.IsNullOrWhiteSpace(isbn))
            throw new ArgumentException("ISBN cannot be empty.", nameof(isbn));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty.", nameof(title));

        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("Author cannot be empty.", nameof(author));

        Id = id;
        Isbn = isbn.Trim();
        Title = title.Trim();
        Author = author.Trim();
    }
}
