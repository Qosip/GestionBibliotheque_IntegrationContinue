using System;
using System.Collections.Generic;
using System.Linq;
using Library.Application.Commands;
using Library.Application.Handlers;
using Library.Application.Repositories;
using Library.Application.Results;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Xunit;

namespace Library.Tests.Application
{
    public sealed class ApplicationAddBookCopyTests
    {
        #region Fakes

        private sealed class FakeBookRepository_ForAddCopy : IBookRepository
        {
            private readonly List<Book> _books = new();

            public IReadOnlyList<Book> Books => _books.AsReadOnly();

            public Book? GetById(Guid id) =>
                _books.SingleOrDefault(b => b.Id == id);

            public IEnumerable<Book> GetAll() =>
                _books.AsReadOnly();

            public void Add(Book book)
            {
                if (book == null) throw new ArgumentNullException(nameof(book));
                _books.Add(book);
            }

            public void Update(Book book)
            {
                if (book == null) throw new ArgumentNullException(nameof(book));

                var index = _books.FindIndex(b => b.Id == book.Id);
                if (index >= 0)
                {
                    _books[index] = book;
                }
            }
        }

        private sealed class FakeSiteRepository_ForAddCopy : ISiteRepository
        {
            private readonly List<Site> _sites = new();

            public IReadOnlyList<Site> Sites => _sites.AsReadOnly();

            public Site? GetById(Guid id) =>
                _sites.SingleOrDefault(s => s.Id == id);

            public IEnumerable<Site> GetAll() =>
                _sites.AsReadOnly();

            public void Add(Site site)
            {
                if (site == null) throw new ArgumentNullException(nameof(site));
                _sites.Add(site);
            }

            public void Update(Site site)
            {
                if (site == null) throw new ArgumentNullException(nameof(site));

                var index = _sites.FindIndex(s => s.Id == site.Id);
                if (index >= 0)
                {
                    _sites[index] = site;
                }
            }
        }

        private sealed class FakeBookCopyRepository_ForAddCopy : IBookCopyRepository
        {
            private readonly List<BookCopy> _copies = new();

            public IReadOnlyList<BookCopy> Copies => _copies.AsReadOnly();

            public BookCopy? GetById(Guid id) =>
                _copies.SingleOrDefault(c => c.Id == id);

            public BookCopy? FindAvailableCopy(Guid bookId, Guid siteId) =>
                _copies.FirstOrDefault(c =>
                    c.BookId == bookId &&
                    c.SiteId == siteId &&
                    c.Status == BookCopyStatus.Available);

            public IEnumerable<BookCopy> GetByBook(Guid bookId) =>
                _copies.Where(c => c.BookId == bookId).ToList();

            public void Add(BookCopy copy)
            {
                if (copy == null) throw new ArgumentNullException(nameof(copy));
                _copies.Add(copy);
            }

            public void Update(BookCopy copy)
            {
                if (copy == null) throw new ArgumentNullException(nameof(copy));

                var index = _copies.FindIndex(c => c.Id == copy.Id);
                if (index >= 0)
                {
                    _copies[index] = copy;
                }
            }
        }

        #endregion

        #region AddBookCopyResult

        [Fact]
        public void AddBookCopyResult_Ok_Should_SetSuccess_BookCopyId_AndNullErrorCode()
        {
            // Intention : vérifier que Ok positionne un succès et propage l’Id de la copie.
            // Arrange
            var copyId = Guid.NewGuid();

            // Act
            var result = AddBookCopyResult.Ok(copyId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(copyId, result.BookCopyId);
            Assert.Null(result.ErrorCode);
        }

        [Fact]
        public void AddBookCopyResult_Fail_Should_SetFailure_EmptyBookCopyId_AndErrorCode()
        {
            // Intention : garantir que Fail renseigne l’erreur et remet l’Id de copie à Guid.Empty.
            // Arrange
            const string errorCode = "ANY_ERROR";

            // Act
            var result = AddBookCopyResult.Fail(errorCode);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(Guid.Empty, result.BookCopyId);
            Assert.Equal(errorCode, result.ErrorCode);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void AddBookCopyResult_Fail_Should_Tolerate_NullOrWhitespace_ErrorCode(string? errorCode)
        {
            // Intention : valider la robustesse de Fail même avec un code d’erreur peu exploitable.
            // Arrange
            // Act
            var result = AddBookCopyResult.Fail(errorCode!);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(Guid.Empty, result.BookCopyId);
            Assert.Equal(errorCode, result.ErrorCode);
        }

        #endregion

        #region AddBookCopyCommand

        [Fact]
        public void AddBookCopyCommand_ParameterlessCtor_Should_Default_Ids_To_Empty()
        {
            // Intention : vérifier les valeurs par défaut de la commande sans paramètre.
            // Arrange
            // Act
            var command = new AddBookCopyCommand();

            // Assert
            Assert.Equal(Guid.Empty, command.BookId);
            Assert.Equal(Guid.Empty, command.SiteId);
        }

        [Fact]
        public void AddBookCopyCommand_ParameterizedCtor_Should_Set_Properties()
        {
            // Intention : garantir que le constructeur paramétré recopie correctement les arguments.
            // Arrange
            var bookId = Guid.NewGuid();
            var siteId = Guid.NewGuid();

            // Act
            var command = new AddBookCopyCommand(bookId, siteId);

            // Assert
            Assert.Equal(bookId, command.BookId);
            Assert.Equal(siteId, command.SiteId);
        }

        [Fact]
        public void AddBookCopyCommand_Properties_Should_Be_Mutable()
        {
            // Intention : vérifier que les propriétés restent modifiables après construction.
            // Arrange
            var command = new AddBookCopyCommand();

            var bookId = Guid.NewGuid();
            var siteId = Guid.NewGuid();

            // Act
            command.BookId = bookId;
            command.SiteId = siteId;

            // Assert
            Assert.Equal(bookId, command.BookId);
            Assert.Equal(siteId, command.SiteId);
        }

        #endregion

        #region AddBookCopyHandler – construction / validation

        [Fact]
        public void AddBookCopyHandler_Ctor_Should_Throw_When_BookRepository_Is_Null()
        {
            // Intention : s’assurer que le handler refuse une dépendance bookRepository null.
            // Arrange
            IBookRepository? bookRepo = null;
            var siteRepo = new FakeSiteRepository_ForAddCopy();
            var copyRepo = new FakeBookCopyRepository_ForAddCopy();

            // Act
            var exception = Assert.Throws<ArgumentNullException>(
                () => new AddBookCopyHandler(bookRepo!, siteRepo, copyRepo));

            // Assert
            Assert.Equal("bookRepository", exception.ParamName);
        }

        [Fact]
        public void AddBookCopyHandler_Ctor_Should_Throw_When_SiteRepository_Is_Null()
        {
            // Intention : s’assurer que le handler refuse une dépendance siteRepository null.
            // Arrange
            var bookRepo = new FakeBookRepository_ForAddCopy();
            ISiteRepository? siteRepo = null;
            var copyRepo = new FakeBookCopyRepository_ForAddCopy();

            // Act
            var exception = Assert.Throws<ArgumentNullException>(
                () => new AddBookCopyHandler(bookRepo, siteRepo!, copyRepo));

            // Assert
            Assert.Equal("siteRepository", exception.ParamName);
        }

        [Fact]
        public void AddBookCopyHandler_Ctor_Should_Throw_When_BookCopyRepository_Is_Null()
        {
            // Intention : s’assurer que le handler refuse une dépendance bookCopyRepository null.
            // Arrange
            var bookRepo = new FakeBookRepository_ForAddCopy();
            var siteRepo = new FakeSiteRepository_ForAddCopy();
            IBookCopyRepository? copyRepo = null;

            // Act
            var exception = Assert.Throws<ArgumentNullException>(
                () => new AddBookCopyHandler(bookRepo, siteRepo, copyRepo!));

            // Assert
            Assert.Equal("bookCopyRepository", exception.ParamName);
        }

        [Fact]
        public void AddBookCopyHandler_Handle_Should_Throw_When_Command_Is_Null()
        {
            // Intention : vérifier que Handle refuse une commande null.
            // Arrange
            var bookRepo = new FakeBookRepository_ForAddCopy();
            var siteRepo = new FakeSiteRepository_ForAddCopy();
            var copyRepo = new FakeBookCopyRepository_ForAddCopy();

            var handler = new AddBookCopyHandler(bookRepo, siteRepo, copyRepo);

            // Act
            var exception = Assert.Throws<ArgumentNullException>(() => handler.Handle(null!));

            // Assert
            Assert.Equal("command", exception.ParamName);
        }

        #endregion

        #region AddBookCopyHandler – erreurs métier

        [Fact]
        public void AddBookCopyHandler_Should_Fail_When_Book_Not_Found()
        {
            // Intention : vérifier le code d’erreur BOOK_NOT_FOUND quand le livre n’existe pas.
            // Arrange
            var bookRepo = new FakeBookRepository_ForAddCopy();   // aucun livre
            var siteRepo = new FakeSiteRepository_ForAddCopy();
            var copyRepo = new FakeBookCopyRepository_ForAddCopy();

            // on crée un site valide, pour isoler l’erreur sur le livre
            var site = new Site(Guid.NewGuid(), "Site A");
            siteRepo.Add(site);

            var command = new AddBookCopyCommand(Guid.NewGuid(), site.Id);
            var handler = new AddBookCopyHandler(bookRepo, siteRepo, copyRepo);

            // Act
            var result = handler.Handle(command);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("BOOK_NOT_FOUND", result.ErrorCode);
            Assert.Equal(Guid.Empty, result.BookCopyId);
            Assert.Empty(copyRepo.Copies);
        }

        [Fact]
        public void AddBookCopyHandler_Should_Fail_When_Site_Not_Found()
        {
            // Intention : vérifier le code d’erreur SITE_NOT_FOUND quand le site n’existe pas.
            // Arrange
            var bookRepo = new FakeBookRepository_ForAddCopy();
            var siteRepo = new FakeSiteRepository_ForAddCopy();
            var copyRepo = new FakeBookCopyRepository_ForAddCopy();

            var book = new Book(Guid.NewGuid(), "9781234567890", "Titre", "Auteur");
            bookRepo.Add(book);

            // pas de site dans le repo
            var command = new AddBookCopyCommand(book.Id, Guid.NewGuid());
            var handler = new AddBookCopyHandler(bookRepo, siteRepo, copyRepo);

            // Act
            var result = handler.Handle(command);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("SITE_NOT_FOUND", result.ErrorCode);
            Assert.Equal(Guid.Empty, result.BookCopyId);
            Assert.Empty(copyRepo.Copies);
        }

        #endregion

        #region AddBookCopyHandler – cas nominal

        [Fact]
        public void AddBookCopyHandler_Should_CreateCopy_And_ReturnOk_When_Book_And_Site_Exist()
        {
            // Intention : valider le scénario nominal de création d’un exemplaire.
            // Arrange
            var bookRepo = new FakeBookRepository_ForAddCopy();
            var siteRepo = new FakeSiteRepository_ForAddCopy();
            var copyRepo = new FakeBookCopyRepository_ForAddCopy();

            var book = new Book(Guid.NewGuid(), "9781234567890", "Titre", "Auteur");
            bookRepo.Add(book);

            var site = new Site(Guid.NewGuid(), "Site A");
            siteRepo.Add(site);

            var command = new AddBookCopyCommand(book.Id, site.Id);
            var handler = new AddBookCopyHandler(bookRepo, siteRepo, copyRepo);

            // Act
            var result = handler.Handle(command);

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.ErrorCode);
            Assert.NotEqual(Guid.Empty, result.BookCopyId);

            Assert.Single(copyRepo.Copies);
            var storedCopy = copyRepo.Copies[0];

            Assert.Equal(result.BookCopyId, storedCopy.Id);
            Assert.Equal(book.Id, storedCopy.BookId);
            Assert.Equal(site.Id, storedCopy.SiteId);
            Assert.Equal(BookCopyStatus.Available, storedCopy.Status);
        }

        #endregion

        #region AddBookCopyHandler – cas improbables / bornes

        [Fact]
        public void AddBookCopyHandler_Should_Throw_When_BookId_Is_Empty_But_Book_Exists_With_Empty_Id()
        {
            // Intention : tester un scénario improbable où un livre a un Id Guid.Empty, ce qui casse BookCopy.
            // Arrange
            var bookRepo = new FakeBookRepository_ForAddCopy();
            var siteRepo = new FakeSiteRepository_ForAddCopy();
            var copyRepo = new FakeBookCopyRepository_ForAddCopy();

            var emptyId = Guid.Empty;
            var book = new Book(emptyId, "9781234567890", "Titre", "Auteur");
            bookRepo.Add(book);

            var site = new Site(Guid.NewGuid(), "Site A");
            siteRepo.Add(site);

            var command = new AddBookCopyCommand(emptyId, site.Id);
            var handler = new AddBookCopyHandler(bookRepo, siteRepo, copyRepo);

            // Act
            var exception = Assert.Throws<ArgumentException>(() => handler.Handle(command));

            // Assert
            Assert.Equal("bookId", exception.ParamName);
            Assert.Empty(copyRepo.Copies);
        }

        [Fact]
        public void AddBookCopyHandler_Should_Throw_When_SiteId_Is_Empty_But_Site_Exists_With_Empty_Id()
        {
            // Intention : tester un scénario improbable où un site a un Id Guid.Empty, ce qui casse BookCopy.
            // Arrange
            var bookRepo = new FakeBookRepository_ForAddCopy();
            var siteRepo = new FakeSiteRepository_ForAddCopy();
            var copyRepo = new FakeBookCopyRepository_ForAddCopy();

            var book = new Book(Guid.NewGuid(), "9781234567890", "Titre", "Auteur");
            bookRepo.Add(book);

            var emptySiteId = Guid.Empty;
            var site = new Site(emptySiteId, "Site A");
            siteRepo.Add(site);

            var command = new AddBookCopyCommand(book.Id, emptySiteId);
            var handler = new AddBookCopyHandler(bookRepo, siteRepo, copyRepo);

            // Act
            var exception = Assert.Throws<ArgumentException>(() => handler.Handle(command));

            // Assert
            Assert.Equal("siteId", exception.ParamName);
            Assert.Empty(copyRepo.Copies);
        }

        #endregion
    }
}
