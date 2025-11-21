using System;
using System.Collections.Generic;
using Library.Application.Commands;
using Library.Application.Handlers;
using Library.Application.Repositories;
using Library.Application.Results;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Xunit;

namespace Library.Tests.Application
{
    public sealed class ApplicationReceiveTransferTests
    {
        #region Fakes

        private sealed class FakeBookCopyRepository : IBookCopyRepository
        {
            private readonly Dictionary<Guid, BookCopy> _store = new();

            public void Add(BookCopy copy)
            {
                _store[copy.Id] = copy;
            }

            public BookCopy? GetById(Guid id)
            {
                _store.TryGetValue(id, out var value);
                return value;
            }

            public BookCopy? FindAvailableCopy(Guid bookId, Guid siteId)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<BookCopy> GetByBook(Guid bookId)
            {
                throw new NotImplementedException();
            }

            public void Update(BookCopy copy)
            {
                _store[copy.Id] = copy;
            }
        }

        #endregion

        #region ReceiveTransferCommand

        [Fact]
        public void ReceiveTransferCommand_Ctor_Should_Set_Properties()
        {
            // Arrange
            var copyId = Guid.NewGuid();
            var targetSiteId = Guid.NewGuid();

            // Act
            var cmd = new ReceiveTransferCommand(copyId, targetSiteId);

            // Assert
            Assert.Equal(copyId, cmd.BookCopyId);
            Assert.Equal(targetSiteId, cmd.TargetSiteId);
        }

        #endregion

        #region ReceiveTransferResult

        [Fact]
        public void ReceiveTransferResult_Ok_Should_Set_Success_And_NullError()
        {
            // Act
            var result = ReceiveTransferResult.Ok();

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.ErrorCode);
        }

        [Fact]
        public void ReceiveTransferResult_Fail_Should_Set_Failure_And_ErrorCode()
        {
            // Arrange
            const string code = "ERROR";

            // Act
            var result = ReceiveTransferResult.Fail(code);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(code, result.ErrorCode);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ReceiveTransferResult_Fail_Should_Accept_NullOrWhitespace(string? code)
        {
            // Act
            var result = ReceiveTransferResult.Fail(code!);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(code, result.ErrorCode);
        }

        #endregion

        #region ReceiveTransferHandler – construction

        [Fact]
        public void ReceiveTransferHandler_Ctor_Should_Throw_When_Repo_Is_Null()
        {
            // Arrange
            IBookCopyRepository? repo = null;

            // Act
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new ReceiveTransferHandler(repo!));

            // Assert
            Assert.Equal("repo", ex.ParamName);
        }

        #endregion

        #region ReceiveTransferHandler – validation Command null

        [Fact]
        public void ReceiveTransferHandler_Handle_Should_Throw_When_Command_Is_Null()
        {
            // Arrange
            var repo = new FakeBookCopyRepository();
            var handler = new ReceiveTransferHandler(repo);

            // Act
            var ex = Assert.Throws<ArgumentNullException>(() =>
                handler.Handle(null!));

            // Assert
            Assert.Equal("command", ex.ParamName);
        }

        #endregion

        #region ReceiveTransferHandler – erreurs métier

        [Fact]
        public void Should_ReturnFail_When_Copy_NotFound()
        {
            // Arrange
            var repo = new FakeBookCopyRepository();
            var handler = new ReceiveTransferHandler(repo);
            var cmd = new ReceiveTransferCommand(Guid.NewGuid(), Guid.NewGuid());

            // Act
            var result = handler.Handle(cmd);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("COPY_NOT_FOUND", result.ErrorCode);
        }

        [Fact]
        public void Should_ReturnFail_When_Copy_Is_Not_InTransfer()
        {
            // Arrange
            var repo = new FakeBookCopyRepository();
            var siteA = Guid.NewGuid();
            var domainCopy = new BookCopy(Guid.NewGuid(), siteA); // Available
            repo.Add(domainCopy);

            var handler = new ReceiveTransferHandler(repo);
            var cmd = new ReceiveTransferCommand(domainCopy.Id, siteA);

            // Act
            var result = handler.Handle(cmd);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("COPY_NOT_IN_TRANSFER", result.ErrorCode);
        }

        #endregion

        #region ReceiveTransferHandler – nominal

        [Fact]
        public void Should_Mark_Copy_AsArrived_And_Persist()
        {
            // Arrange
            var repo = new FakeBookCopyRepository();

            var siteA = Guid.NewGuid();
            var siteB = Guid.NewGuid();

            // copy in transfer
            var domainCopy = new BookCopy(Guid.NewGuid(), siteA);
            domainCopy.MarkAsInTransfer();

            repo.Add(domainCopy);

            var handler = new ReceiveTransferHandler(repo);
            var cmd = new ReceiveTransferCommand(domainCopy.Id, siteB);

            // Act
            var result = handler.Handle(cmd);

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.ErrorCode);

            var updated = repo.GetById(domainCopy.Id);
            Assert.NotNull(updated);
            Assert.Equal(siteB, updated!.SiteId);
            Assert.Equal(BookCopyStatus.Available, updated.Status);
        }

        #endregion

        #region ReceiveTransferHandler – cas limites

        [Fact]
        public void Should_Handle_NewSite_Same_As_OldSite()
        {
            // Arrange
            var repo = new FakeBookCopyRepository();

            var site = Guid.NewGuid();
            var copy = new BookCopy(Guid.NewGuid(), site);
            copy.MarkAsInTransfer();
            repo.Add(copy);

            var handler = new ReceiveTransferHandler(repo);
            var cmd = new ReceiveTransferCommand(copy.Id, site);

            // Act
            var result = handler.Handle(cmd);

            // Assert
            Assert.True(result.Success);

            var updated = repo.GetById(copy.Id);
            Assert.Equal(site, updated!.SiteId);
            Assert.Equal(BookCopyStatus.Available, updated.Status);
        }

        [Fact]
        public void Should_Fail_When_NewSite_Is_EmptyGuid()
        {
            // Arrange
            var repo = new FakeBookCopyRepository();

            var siteA = Guid.NewGuid();
            var copy = new BookCopy(Guid.NewGuid(), siteA);
            copy.MarkAsInTransfer();
            repo.Add(copy);

            var handler = new ReceiveTransferHandler(repo);
            var cmd = new ReceiveTransferCommand(copy.Id, Guid.Empty);

            // Act
            var result = handler.Handle(cmd);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("ArgumentException", result.ErrorCode);
        }

        [Fact]
        public void Should_Not_Throw_On_Extremely_Large_Number_Of_Calls()
        {
            // Arrange
            var repo = new FakeBookCopyRepository();

            var site = Guid.NewGuid();
            var target = Guid.NewGuid();

            var copy = new BookCopy(Guid.NewGuid(), site);
            copy.MarkAsInTransfer();
            repo.Add(copy);

            var handler = new ReceiveTransferHandler(repo);

            // Act
            for (int i = 0; i < 5_000; i++)
            {
                var r = handler.Handle(new ReceiveTransferCommand(copy.Id, target));
                // Once received the first time, any other call should fail COPY_NOT_IN_TRANSFER
            }

            // Assert no exception thrown
            Assert.True(true);
        }

        #endregion

        #region ReceiveTransferHandler – improbable

        [Fact]
        public void Should_Not_Update_When_Repo_Throws_In_Update()
        {
            // Arrange
            var copy = new BookCopy(Guid.NewGuid(), Guid.NewGuid());
            copy.MarkAsInTransfer();

            var fakeRepo = new FakeRepoThrowOnUpdate(copy);

            var handler = new ReceiveTransferHandler(fakeRepo);
            var cmd = new ReceiveTransferCommand(copy.Id, Guid.NewGuid());

            // Act
            var result = handler.Handle(cmd);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Exception", result.ErrorCode);
        }

        private sealed class FakeRepoThrowOnUpdate : IBookCopyRepository
        {
            private readonly BookCopy _copy;

            public FakeRepoThrowOnUpdate(BookCopy copy)
            {
                _copy = copy;
            }

            public void Add(BookCopy copy) { }
            public BookCopy? FindAvailableCopy(Guid bookId, Guid siteId) => null;
            public IEnumerable<BookCopy> GetByBook(Guid bookId) => Array.Empty<BookCopy>();
            public BookCopy? GetById(Guid id) => _copy;

            public void Update(BookCopy copy)
            {
                throw new Exception();
            }
        }

        #endregion
    }
}
