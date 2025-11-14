using System;
using System.Collections.Generic;
using Library.Application;
using Library.Domain;
using Xunit;

namespace Library.Tests;

public class RequestTransferHandlerTests
{
    private sealed class FakeBookCopyRepository : IBookCopyRepository
    {
        private readonly List<BookCopy> _copies = new();

        public void Add(BookCopy copy) => _copies.Add(copy);

        public BookCopy? FindAvailableCopy(Guid bookId, Guid siteId)
        {
            foreach (var copy in _copies)
            {
                if (copy.BookId == bookId &&
                    copy.SiteId == siteId &&
                    copy.Status == BookCopyStatus.Available)
                {
                    return copy;
                }
            }
            return null;
        }

        public BookCopy? GetById(Guid id)
        {
            foreach (var copy in _copies)
            {
                if (copy.Id == id)
                    return copy;
            }
            return null;
        }
    }

    [Fact]
    public void Handle_puts_copy_in_transfer_when_available_on_source_site()
    {
        // Arrange
        var repo = new FakeBookCopyRepository();
        var handler = new RequestTransferHandler(repo);

        var bookId = Guid.NewGuid();
        var sourceSiteId = Guid.NewGuid();
        var targetSiteId = Guid.NewGuid();

        var copy = new BookCopy(bookId, sourceSiteId);
        repo.Add(copy);

        var command = new RequestTransferCommand(bookId, sourceSiteId, targetSiteId);

        // Act
        var result = handler.Handle(command);

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.ErrorCode);
        Assert.Equal(copy.Id, result.BookCopyId);
        Assert.Equal(BookCopyStatus.InTransfer, copy.Status);
    }

    [Fact]
    public void Handle_returns_error_when_no_copy_available_on_source_site()
    {
        // Arrange
        var repo = new FakeBookCopyRepository();
        var handler = new RequestTransferHandler(repo);

        var bookId = Guid.NewGuid();
        var sourceSiteId = Guid.NewGuid();
        var targetSiteId = Guid.NewGuid();

        // On ajoute une copie mais sur un autre site ou pas disponible
        var otherSiteId = Guid.NewGuid();
        var copy = new BookCopy(bookId, otherSiteId);
        copy.MarkAsInTransfer(); // ou MarkAsBorrowed
        repo.Add(copy);

        var command = new RequestTransferCommand(bookId, sourceSiteId, targetSiteId);

        // Act
        var result = handler.Handle(command);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("NO_COPY_AVAILABLE_AT_SOURCE_SITE", result.ErrorCode);
        Assert.Equal(Guid.Empty, result.BookCopyId);
    }

    [Fact]
    public void Handle_returns_error_when_source_and_target_sites_are_same()
    {
        // Arrange
        var repo = new FakeBookCopyRepository();
        var handler = new RequestTransferHandler(repo);

        var siteId = Guid.NewGuid();
        var command = new RequestTransferCommand(
            bookId: Guid.NewGuid(),
            sourceSiteId: siteId,
            targetSiteId: siteId);

        // Act
        var result = handler.Handle(command);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("SOURCE_AND_TARGET_MUST_DIFFER", result.ErrorCode);
    }

}
