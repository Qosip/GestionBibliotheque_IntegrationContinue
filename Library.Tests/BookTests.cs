using System;
using Library.Domain;
using Xunit;

namespace Library.Tests;

public class BookTests
{
    [Fact]
    public void Can_create_book_with_valid_data()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var book = new Book(id, "9781234567890", "Clean Code", "Robert C. Martin");

        // Assert
        Assert.Equal(id, book.Id);
        Assert.Equal("9781234567890", book.Isbn);
        Assert.Equal("Clean Code", book.Title);
        Assert.Equal("Robert C. Martin", book.Author);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Constructor_throws_when_title_is_null_or_whitespace(string? title)
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act + Assert
        Assert.Throws<ArgumentException>(() =>
        {
            _ = new Book(id, "9781234567890", title!, "Author");
        });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Constructor_throws_when_isbn_is_null_or_whitespace(string? isbn)
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act + Assert
        Assert.Throws<ArgumentException>(() =>
        {
            _ = new Book(id, isbn!, "Title", "Author");
        });
    }
}
