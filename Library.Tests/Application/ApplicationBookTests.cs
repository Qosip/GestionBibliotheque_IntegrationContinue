using System;
using System.Collections.Generic;
using Library.Application.Commands;
using Library.Application.Handlers;
using Library.Application.Repositories;
using Library.Application.Results;
using Library.Domain.Entities;
using Xunit;

namespace Library.Tests.Application
{
    public sealed class ApplicationBookTests
    {
        #region Fakes

        private sealed class FakeBookRepository : IBookRepository
        {
            private readonly List<Book> _books = new();

            public IReadOnlyList<Book> Books => _books.AsReadOnly();

            public void Add(Book book)
            {
                if (book == null) throw new ArgumentNullException(nameof(book));
                _books.Add(book);
            }

            public IEnumerable<Book> GetAll()
            {
                throw new NotImplementedException();
            }

            public Book? GetById(Guid id)
            {
                throw new NotImplementedException();
            }

            public void Update(Book book)
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region RegisterBookResult

        [Fact]
        public void RegisterBookResult_Ok_Should_Set_Success_BookId_And_NullErrorCode()
        {
            // Arrange
            var bookId = Guid.NewGuid();

            // Act
            var result = RegisterBookResult.Ok(bookId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(bookId, result.BookId);
            Assert.Null(result.ErrorCode);
        }

        [Fact]
        public void RegisterBookResult_Fail_Should_Set_Failure_EmptyBookId_And_ErrorCode()
        {
            // Arrange
            const string errorCode = "ANY_ERROR";

            // Act
            var result = RegisterBookResult.Fail(errorCode);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(Guid.Empty, result.BookId);
            Assert.Equal(errorCode, result.ErrorCode);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void RegisterBookResult_Fail_Should_Accept_NullOrWhitespace_ErrorCode(string? errorCode)
        {
            // Arrange
            // Act
            var result = RegisterBookResult.Fail(errorCode!);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(Guid.Empty, result.BookId);
            Assert.Equal(errorCode, result.ErrorCode);
        }

        #endregion

        #region RegisterBookCommand

        [Fact]
        public void RegisterBookCommand_ParameterlessCtor_Should_Default_To_EmptyStrings()
        {
            // Arrange
            // Act
            var command = new RegisterBookCommand();

            // Assert
            Assert.Equal(string.Empty, command.Isbn);
            Assert.Equal(string.Empty, command.Title);
            Assert.Equal(string.Empty, command.Author);
        }

        [Fact]
        public void RegisterBookCommand_ParameterizedCtor_Should_Set_Properties()
        {
            // Arrange
            const string isbn = "9781234567890";
            const string title = "Titre";
            const string author = "Auteur";

            // Act
            var command = new RegisterBookCommand(isbn, title, author);

            // Assert
            Assert.Equal(isbn, command.Isbn);
            Assert.Equal(title, command.Title);
            Assert.Equal(author, command.Author);
        }

        [Fact]
        public void RegisterBookCommand_Properties_Should_Be_Mutable()
        {
            // Arrange
            var command = new RegisterBookCommand();

            // Act
            command.Isbn = "X";
            command.Title = "Y";
            command.Author = "Z";

            // Assert
            Assert.Equal("X", command.Isbn);
            Assert.Equal("Y", command.Title);
            Assert.Equal("Z", command.Author);
        }

        #endregion

        #region RegisterBookHandler – construction

        [Fact]
        public void RegisterBookHandler_Ctor_Should_Throw_When_Repository_Is_Null()
        {
            // Arrange
            IBookRepository? repo = null;

            // Act
            var ex = Assert.Throws<ArgumentNullException>(() => new RegisterBookHandler(repo!));

            // Assert
            Assert.Equal("bookRepository", ex.ParamName);
        }

        #endregion

        #region RegisterBookHandler – Handle validation

        [Fact]
        public void RegisterBookHandler_Handle_Should_Throw_When_Command_Is_Null()
        {
            // Arrange
            var repo = new FakeBookRepository();
            var handler = new RegisterBookHandler(repo);

            // Act
            var ex = Assert.Throws<ArgumentNullException>(() => handler.Handle(null!));

            // Assert
            Assert.Equal("command", ex.ParamName);
        }

        [Theory]
        [InlineData(null, "Title", "Author")]
        [InlineData("", "Title", "Author")]
        [InlineData("   ", "Title", "Author")]
        [InlineData("9781234567890", null, "Author")]
        [InlineData("9781234567890", "", "Author")]
        [InlineData("9781234567890", "   ", "Author")]
        [InlineData("9781234567890", "Title", null)]
        [InlineData("9781234567890", "Title", "")]
        [InlineData("9781234567890", "Title", "   ")]
        public void RegisterBookHandler_Should_ReturnFail_And_NotPersist_When_Data_Invalid(
            string? isbn,
            string? title,
            string? author)
        {
            // Arrange
            var repo = new FakeBookRepository();
            var handler = new RegisterBookHandler(repo);

            var command = new RegisterBookCommand
            {
                Isbn = isbn!,
                Title = title!,
                Author = author!
            };

            // Act
            var result = handler.Handle(command);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(Guid.Empty, result.BookId);
            Assert.Equal("INVALID_BOOK_DATA", result.ErrorCode);
            Assert.Empty(repo.Books);
        }

        [Fact]
        public void RegisterBookHandler_Should_Treat_DefaultCommand_As_Invalid()
        {
            // Arrange
            var repo = new FakeBookRepository();
            var handler = new RegisterBookHandler(repo);
            var command = new RegisterBookCommand(); // Isbn/Title/Author = string.Empty

            // Act
            var result = handler.Handle(command);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(Guid.Empty, result.BookId);
            Assert.Equal("INVALID_BOOK_DATA", result.ErrorCode);
            Assert.Empty(repo.Books);
        }

        #endregion

        #region RegisterBookHandler – cas nominal

        [Fact]
        public void RegisterBookHandler_Should_PersistBook_And_ReturnOk_When_Data_Valid()
        {
            // Arrange
            var repo = new FakeBookRepository();
            var handler = new RegisterBookHandler(repo);

            const string isbn = "9781234567890";
            const string title = "Clean Code";
            const string author = "Robert C. Martin";

            var command = new RegisterBookCommand(isbn, title, author);

            // Act
            var result = handler.Handle(command);

            // Assert
            Assert.True(result.Success);
            Assert.NotEqual(Guid.Empty, result.BookId);
            Assert.Null(result.ErrorCode);

            Assert.Single(repo.Books);
            var stored = repo.Books[0];

            Assert.Equal(result.BookId, stored.Id);
            Assert.Equal(isbn, stored.Isbn);
            Assert.Equal(title, stored.Title);
            Assert.Equal(author, stored.Author);
        }

        #endregion

        #region RegisterBookHandler – cas improbables / bornes

        [Fact]
        public void RegisterBookHandler_Should_Handle_VeryLongStrings_Without_Exception()
        {
            // Arrange
            var repo = new FakeBookRepository();
            var handler = new RegisterBookHandler(repo);

            var veryLongIsbn = new string('9', 1_000);
            var veryLongTitle = new string('T', 10_000);
            var veryLongAuthor = new string('A', 10_000);

            var command = new RegisterBookCommand(veryLongIsbn, veryLongTitle, veryLongAuthor);

            // Act
            var exception = Record.Exception(() => handler.Handle(command));

            // Assert
            Assert.Null(exception);
            Assert.Single(repo.Books);

            var stored = repo.Books[0];
            Assert.Equal(veryLongIsbn, stored.Isbn);
            Assert.Equal(veryLongTitle, stored.Title);
            Assert.Equal(veryLongAuthor, stored.Author);
        }

        [Fact]
        public void RegisterBookHandler_Should_Allow_DuplicateBooks_With_Same_Data_But_DifferentIds()
        {
            // Arrange
            var repo = new FakeBookRepository();
            var handler = new RegisterBookHandler(repo);

            const string isbn = "9781234567890";
            const string title = "Titre";
            const string author = "Auteur";

            var command = new RegisterBookCommand(isbn, title, author);

            // Act
            var result1 = handler.Handle(command);
            var result2 = handler.Handle(command);

            // Assert
            Assert.True(result1.Success);
            Assert.True(result2.Success);
            Assert.NotEqual(Guid.Empty, result1.BookId);
            Assert.NotEqual(Guid.Empty, result2.BookId);
            Assert.NotEqual(result1.BookId, result2.BookId);

            Assert.Equal(2, repo.Books.Count);
            Assert.All(repo.Books, b =>
            {
                Assert.Equal(isbn, b.Isbn);
                Assert.Equal(title, b.Title);
                Assert.Equal(author, b.Author);
            });
            Assert.NotEqual(repo.Books[0].Id, repo.Books[1].Id);
        }

        #endregion
    }
}
