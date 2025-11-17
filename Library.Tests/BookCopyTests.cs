using System;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Xunit;

namespace Library.Tests.Domain;

/// <summary>
/// Tests métier de la machine à états d’un exemplaire (BookCopy).
/// Objectif : garantir la cohérence stricte des transitions et des règles
/// associées aux mouvements, emprunts, retours et transferts.
/// </summary>
public sealed class BookCopyTests
{
    private static Guid AnyBookId() => Guid.NewGuid();
    private static Guid AnySiteId() => Guid.NewGuid();

    private static BookCopy CreateAvailableCopy() =>
        new BookCopy(AnyBookId(), AnySiteId());

    // -----------------------------------------------------------------------
    // 1. Création
    // -----------------------------------------------------------------------

    [Fact]
    public void Constructor_Should_Create_Available_Copy()
    {
        var bookId = AnyBookId();
        var siteId = AnySiteId();

        var copy = new BookCopy(bookId, siteId);

        Assert.Equal(bookId, copy.BookId);
        Assert.Equal(siteId, copy.SiteId);
        Assert.Equal(BookCopyStatus.Available, copy.Status);
        Assert.NotEqual(Guid.Empty, copy.Id);
    }

    [Fact]
    public void Constructor_Should_Throw_When_BookId_Empty()
    {
        Assert.Throws<ArgumentException>(() =>
            new BookCopy(Guid.Empty, AnySiteId()));
    }

    [Fact]
    public void Constructor_Should_Throw_When_SiteId_Empty()
    {
        Assert.Throws<ArgumentException>(() =>
            new BookCopy(AnyBookId(), Guid.Empty));
    }

    // -----------------------------------------------------------------------
    // 2. Emprunt (Available → Borrowed)
    // -----------------------------------------------------------------------

    [Fact]
    public void MarkAsBorrowed_Should_Set_Status_To_Borrowed_When_Available()
    {
        var copy = CreateAvailableCopy();

        copy.MarkAsBorrowed();

        Assert.Equal(BookCopyStatus.Borrowed, copy.Status);
    }

    [Fact]
    public void MarkAsBorrowed_Should_Throw_When_Not_Available()
    {
        var copy = CreateAvailableCopy();
        copy.MarkAsBorrowed(); // devient Borrowed

        Assert.Throws<InvalidOperationException>(() =>
            copy.MarkAsBorrowed());
    }

    // -----------------------------------------------------------------------
    // 3. Retour (Borrowed → Available)
    // -----------------------------------------------------------------------

    [Fact]
    public void MarkAsReturned_Should_Set_Status_To_Available_When_Borrowed()
    {
        var copy = CreateAvailableCopy();
        copy.MarkAsBorrowed();

        copy.MarkAsReturned();

        Assert.Equal(BookCopyStatus.Available, copy.Status);
    }

    [Fact]
    public void MarkAsReturned_Should_Throw_When_Not_Borrowed()
    {
        var copy = CreateAvailableCopy(); // Already Available

        Assert.Throws<InvalidOperationException>(() =>
            copy.MarkAsReturned());
    }

    // -----------------------------------------------------------------------
    // 4. Transfert (Available → InTransfer → Arrived)
    // -----------------------------------------------------------------------

    [Fact]
    public void MarkAsInTransfer_Should_Set_Status_To_InTransfer_When_Available()
    {
        var copy = CreateAvailableCopy();

        copy.MarkAsInTransfer();

        Assert.Equal(BookCopyStatus.InTransfer, copy.Status);
    }

    [Fact]
    public void MarkAsInTransfer_Should_Throw_When_Not_Available()
    {
        var copy = CreateAvailableCopy();
        copy.MarkAsBorrowed();

        Assert.Throws<InvalidOperationException>(() =>
            copy.MarkAsInTransfer());
    }

    [Fact]
    public void MarkAsArrived_Should_Set_New_Site_And_Reset_Status_To_Available()
    {
        var copy = CreateAvailableCopy();
        copy.MarkAsInTransfer();

        var newSiteId = AnySiteId();

        copy.MarkAsArrived(newSiteId);

        Assert.Equal(BookCopyStatus.Available, copy.Status);
        Assert.Equal(newSiteId, copy.SiteId);
    }

    [Fact]
    public void MarkAsArrived_Should_Throw_When_Not_InTransfer()
    {
        var copy = CreateAvailableCopy(); // Available

        Assert.Throws<InvalidOperationException>(() =>
            copy.MarkAsArrived(AnySiteId()));
    }

    [Fact]
    public void MarkAsArrived_Should_Throw_When_NewSiteId_Empty()
    {
        var copy = CreateAvailableCopy();
        copy.MarkAsInTransfer();

        Assert.Throws<ArgumentException>(() =>
            copy.MarkAsArrived(Guid.Empty));
    }

    // -----------------------------------------------------------------------
    // 5. Déplacement direct de site (MoveToSite)
    // -----------------------------------------------------------------------

    [Fact]
    public void MoveToSite_Should_Update_SiteId()
    {
        var copy = CreateAvailableCopy();
        var newSite = AnySiteId();

        copy.MoveToSite(newSite);

        Assert.Equal(newSite, copy.SiteId);
    }

    [Fact]
    public void MoveToSite_Should_Throw_When_SiteId_Empty()
    {
        var copy = CreateAvailableCopy();

        Assert.Throws<ArgumentException>(() =>
            copy.MoveToSite(Guid.Empty));
    }
}
