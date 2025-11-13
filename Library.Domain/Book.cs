using System;

namespace Library.Domain;

public class Book
{
    public Guid Id { get; }
    public string Isbn { get; }
    public string Title { get; }
    public string Author { get; }

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
