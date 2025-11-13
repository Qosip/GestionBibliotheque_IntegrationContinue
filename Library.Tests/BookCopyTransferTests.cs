using System;
using Library.Domain;
using Xunit;

namespace Library.Tests;

public class BookCopyTransferTests
{
    [Fact]
    public void MarkAsInTransfer_sets_status_to_InTransfer_when_available()
    {
        // Arrange
        var copy = new BookCopy(Guid.NewGuid(), Guid.NewGuid());

        // Act
        copy.MarkAsInTransfer();

        // Assert
        Assert.Equal(BookCopyStatus.InTransfer, copy.Status);
    }

    [Fact]
    public void MarkAsInTransfer_throws_when_not_available()
    {
        // Arrange
        var copy = new BookCopy(Guid.NewGuid(), Guid.NewGuid());
        copy.MarkAsBorrowed();

        // Act + Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            copy.MarkAsInTransfer();
        });
    }

    [Fact]
    public void MarkAsArrived_sets_status_to_Available_and_updates_site_when_in_transfer()
    {
        // Arrange
        var originalSiteId = Guid.NewGuid();
        var targetSiteId = Guid.NewGuid();
        var copy = new BookCopy(Guid.NewGuid(), originalSiteId);
        copy.MarkAsInTransfer();

        // Act
        copy.MarkAsArrived(targetSiteId);

        // Assert
        Assert.Equal(BookCopyStatus.Available, copy.Status);
        Assert.Equal(targetSiteId, copy.SiteId);
    }

    [Fact]
    public void MarkAsArrived_throws_when_not_in_transfer()
    {
        // Arrange
        var copy = new BookCopy(Guid.NewGuid(), Guid.NewGuid());

        // Act + Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            copy.MarkAsArrived(Guid.NewGuid());
        });
    }
}
