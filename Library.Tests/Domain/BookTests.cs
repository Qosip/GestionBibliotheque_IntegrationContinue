using System;
using Library.Domain;
using Library.Domain.Entities;
using Xunit;

namespace Library.Tests.Domain;

/// <summary>
/// Tests métier pour l’entité Book.
/// Couvre :
/// - invariants sur ISBN / Titre / Auteur,
/// - nettoyage des données entrantes,
/// - stabilité de l’identité.
/// </summary>
public sealed class BookTests
{
    // ------------------------------------------------------------
    // 1) Invariants – paramètres obligatoires
    // ------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_Should_Throw_When_Isbn_Invalid(string? isbn)
    {
        Assert.Throws<ArgumentException>(() =>
            new Book(Guid.NewGuid(), isbn!, "Title", "Author"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_Should_Throw_When_Title_Invalid(string? title)
    {
        Assert.Throws<ArgumentException>(() =>
            new Book(Guid.NewGuid(), "ISBN", title!, "Author"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_Should_Throw_When_Author_Invalid(string? author)
    {
        Assert.Throws<ArgumentException>(() =>
            new Book(Guid.NewGuid(), "ISBN", "Title", author!));
    }

    // ------------------------------------------------------------
    // 2) Nettoyage – trimming automatique
    // ------------------------------------------------------------

    [Fact]
    public void Ctor_Should_Trim_Fields()
    {
        var book = new Book(
            Guid.NewGuid(),
            " 978-1111111111 ",
            "  Le Titre  ",
            "  L’Auteur "
        );

        Assert.Equal("978-1111111111", book.Isbn);
        Assert.Equal("Le Titre", book.Title);
        Assert.Equal("L’Auteur", book.Author);
    }

    // ------------------------------------------------------------
    // 3) Intégrité de l’identité
    // ------------------------------------------------------------

    [Fact]
    public void Ctor_Should_Set_Id_Exactly_As_Provided()
    {
        var id = Guid.NewGuid();

        var book = new Book(id, "ISBN", "Title", "Author");

        Assert.Equal(id, book.Id);
    }
}
