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
    public sealed class ApplicationTransferTests
    {
        #region Fakes

        private sealed class FakeBookCopyRepository : IBookCopyRepository
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

        #region RequestTransferResult

        [Fact]
        public void RequestTransferResult_Ok_Should_SetSuccess_BookCopyId_AndNullErrorCode()
        {
            // Intention : vérifier que Ok positionne un succès et propage l’Id de la copie.
            // Arrange
            var copyId = Guid.NewGuid();

            // Act
            var result = RequestTransferResult.Ok(copyId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(copyId, result.BookCopyId);
            Assert.Null(result.ErrorCode);
        }

        [Fact]
        public void RequestTransferResult_Fail_Should_SetFailure_EmptyBookCopyId_AndErrorCode()
        {
            // Intention : garantir que Fail renseigne l’erreur et remet l’Id de copie à Guid.Empty.
            // Arrange
            const string errorCode = "ANY_ERROR";

            // Act
            var result = RequestTransferResult.Fail(errorCode);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(Guid.Empty, result.BookCopyId);
            Assert.Equal(errorCode, result.ErrorCode);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void RequestTransferResult_Fail_Should_Tolerate_NullOrWhitespace_ErrorCode(string? errorCode)
        {
            // Intention : valider la robustesse de Fail même avec un code d’erreur peu exploitable.
            // Arrange
            // Act
            var result = RequestTransferResult.Fail(errorCode!);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(Guid.Empty, result.BookCopyId);
            Assert.Equal(errorCode, result.ErrorCode);
        }

        #endregion

        #region RequestTransferCommand

        [Fact]
        public void RequestTransferCommand_ParameterlessCtor_Should_Default_Ids_To_Empty()
        {
            // Intention : vérifier les valeurs par défaut de la commande sans paramètre.
            // Arrange
            // Act
            var command = new RequestTransferCommand();

            // Assert
            Assert.Equal(Guid.Empty, command.BookId);
            Assert.Equal(Guid.Empty, command.SourceSiteId);
            Assert.Equal(Guid.Empty, command.TargetSiteId);
        }

        [Fact]
        public void RequestTransferCommand_ParameterizedCtor_Should_Set_Properties()
        {
            // Intention : garantir que le constructeur paramétré recopie correctement les arguments.
            // Arrange
            var bookId = Guid.NewGuid();
            var sourceSiteId = Guid.NewGuid();
            var targetSiteId = Guid.NewGuid();

            // Act
            var command = new RequestTransferCommand(bookId, sourceSiteId, targetSiteId);

            // Assert
            Assert.Equal(bookId, command.BookId);
            Assert.Equal(sourceSiteId, command.SourceSiteId);
            Assert.Equal(targetSiteId, command.TargetSiteId);
        }

        [Fact]
        public void RequestTransferCommand_Properties_Should_Be_Mutable()
        {
            // Intention : vérifier que les propriétés restent modifiables après construction.
            // Arrange
            var command = new RequestTransferCommand();

            var bookId = Guid.NewGuid();
            var sourceSiteId = Guid.NewGuid();
            var targetSiteId = Guid.NewGuid();

            // Act
            command.BookId = bookId;
            command.SourceSiteId = sourceSiteId;
            command.TargetSiteId = targetSiteId;

            // Assert
            Assert.Equal(bookId, command.BookId);
            Assert.Equal(sourceSiteId, command.SourceSiteId);
            Assert.Equal(targetSiteId, command.TargetSiteId);
        }

        #endregion

        #region RequestTransferHandler – construction / validation

        [Fact]
        public void RequestTransferHandler_Ctor_Should_Throw_When_Repository_Is_Null()
        {
            // Intention : s’assurer que le handler refuse une dépendance null explicite.
            // Arrange
            IBookCopyRepository? repo = null;

            // Act
            var exception = Assert.Throws<ArgumentNullException>(() => new RequestTransferHandler(repo!));

            // Assert
            Assert.Equal("bookCopyRepository", exception.ParamName);
        }

        [Fact]
        public void RequestTransferHandler_Handle_Should_Throw_When_Command_Is_Null()
        {
            // Intention : garantir qu’un appel avec une commande null est rejeté clairement.
            // Arrange
            var repo = new FakeBookCopyRepository();
            var handler = new RequestTransferHandler(repo);

            // Act
            var exception = Assert.Throws<ArgumentNullException>(() => handler.Handle(null!));

            // Assert
            Assert.Equal("command", exception.ParamName);
        }

        [Fact]
        public void RequestTransferHandler_Should_Fail_When_Source_And_Target_Sites_Are_Equal()
        {
            // Intention : vérifier la règle métier SOURCE_AND_TARGET_MUST_DIFFER.
            // Arrange
            var repo = new FakeBookCopyRepository();
            var handler = new RequestTransferHandler(repo);

            var bookId = Guid.NewGuid();
            var sameSiteId = Guid.NewGuid();

            var command = new RequestTransferCommand(bookId, sameSiteId, sameSiteId);

            // Act
            var result = handler.Handle(command);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(Guid.Empty, result.BookCopyId);
            Assert.Equal("SOURCE_AND_TARGET_MUST_DIFFER", result.ErrorCode);
            Assert.Empty(repo.Copies);
        }

        [Fact]
        public void RequestTransferHandler_Should_Fail_When_Source_And_Target_Sites_Are_Both_Empty()
        {
            // Intention : couvrir le cas limite où les deux sites sont Guid.Empty et donc égaux.
            // Arrange
            var repo = new FakeBookCopyRepository();
            var handler = new RequestTransferHandler(repo);

            var bookId = Guid.NewGuid();
            var command = new RequestTransferCommand(bookId, Guid.Empty, Guid.Empty);

            // Act
            var result = handler.Handle(command);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("SOURCE_AND_TARGET_MUST_DIFFER", result.ErrorCode);
            Assert.Equal(Guid.Empty, result.BookCopyId);
        }

        #endregion

        #region RequestTransferHandler – erreurs métier

        [Fact]
        public void RequestTransferHandler_Should_Fail_When_No_Available_Copy_At_Source_Site()
        {
            // Intention : vérifier le comportement quand aucune copie disponible n’est trouvée au site source.
            // Arrange
            var repo = new FakeBookCopyRepository();
            var handler = new RequestTransferHandler(repo);

            var bookId = Guid.NewGuid();
            var sourceSiteId = Guid.NewGuid();
            var targetSiteId = Guid.NewGuid();

            // aucune copy dans le repo
            var command = new RequestTransferCommand(bookId, sourceSiteId, targetSiteId);

            // Act
            var result = handler.Handle(command);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("NO_COPY_AVAILABLE_AT_SOURCE_SITE", result.ErrorCode);
            Assert.Equal(Guid.Empty, result.BookCopyId);
        }

        [Fact]
        public void RequestTransferHandler_Should_Fail_When_Copy_Exists_On_Another_Site()
        {
            // Intention : tester un cas très fréquent où la copy existe mais sur un autre site.
            // Arrange
            var repo = new FakeBookCopyRepository();
            var handler = new RequestTransferHandler(repo);

            var bookId = Guid.NewGuid();
            var sourceSiteId = Guid.NewGuid();
            var otherSiteId = Guid.NewGuid();
            var targetSiteId = Guid.NewGuid();

            // Copy disponible mais sur un autre site
            var copyOnOtherSite = new BookCopy(bookId, otherSiteId);
            repo.Add(copyOnOtherSite);

            var command = new RequestTransferCommand(bookId, sourceSiteId, targetSiteId);

            // Act
            var result = handler.Handle(command);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("NO_COPY_AVAILABLE_AT_SOURCE_SITE", result.ErrorCode);
            Assert.Equal(Guid.Empty, result.BookCopyId);

            // et la copy existante ne doit pas changer de statut
            Assert.Equal(BookCopyStatus.Available, copyOnOtherSite.Status);
        }

        [Fact]
        public void RequestTransferHandler_Should_Fail_When_Only_NonAvailable_Copies_Exist_At_Source()
        {
            // Intention : valider que seules les copies Available sont éligibles au transfert.
            // Arrange
            var repo = new FakeBookCopyRepository();
            var handler = new RequestTransferHandler(repo);

            var bookId = Guid.NewGuid();
            var sourceSiteId = Guid.NewGuid();
            var targetSiteId = Guid.NewGuid();

            // Copy sur le bon site mais déjà empruntée ou en transfert
            var borrowedCopy = new BookCopy(bookId, sourceSiteId);
            borrowedCopy.MarkAsBorrowed();
            repo.Add(borrowedCopy);

            var inTransferCopy = new BookCopy(bookId, sourceSiteId);
            inTransferCopy.MarkAsInTransfer();
            repo.Add(inTransferCopy);

            var command = new RequestTransferCommand(bookId, sourceSiteId, targetSiteId);

            // Act
            var result = handler.Handle(command);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("NO_COPY_AVAILABLE_AT_SOURCE_SITE", result.ErrorCode);
            Assert.Equal(Guid.Empty, result.BookCopyId);
            Assert.Equal(BookCopyStatus.Borrowed, borrowedCopy.Status);
            Assert.Equal(BookCopyStatus.InTransfer, inTransferCopy.Status);
        }

        #endregion

        #region RequestTransferHandler – cas nominal

        [Fact]
        public void RequestTransferHandler_Should_Mark_Copy_InTransfer_And_Return_Ok_When_Found()
        {
            // Intention : valider le scénario nominal d’une demande de transfert réussie.
            // Arrange
            var repo = new FakeBookCopyRepository();
            var handler = new RequestTransferHandler(repo);

            var bookId = Guid.NewGuid();
            var sourceSiteId = Guid.NewGuid();
            var targetSiteId = Guid.NewGuid();

            var availableCopy = new BookCopy(bookId, sourceSiteId); // Status = Available
            repo.Add(availableCopy);

            var command = new RequestTransferCommand(bookId, sourceSiteId, targetSiteId);

            // Act
            var result = handler.Handle(command);

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.ErrorCode);
            Assert.Equal(availableCopy.Id, result.BookCopyId);

            // la copy doit être passée en InTransfer
            var storedCopy = repo.Copies.Single(c => c.Id == availableCopy.Id);
            Assert.Equal(BookCopyStatus.InTransfer, storedCopy.Status);
        }

        #endregion

        #region RequestTransferHandler – cas improbables / multi-copies

        [Fact]
        public void RequestTransferHandler_Should_Select_First_Available_Copy_When_Multiple_Exist()
        {
            // Intention : vérifier le comportement en présence de plusieurs copies valides sur le site source.
            // Arrange
            var repo = new FakeBookCopyRepository();
            var handler = new RequestTransferHandler(repo);

            var bookId = Guid.NewGuid();
            var sourceSiteId = Guid.NewGuid();
            var targetSiteId = Guid.NewGuid();

            // Copy 1 : Available
            var copy1 = new BookCopy(bookId, sourceSiteId);
            // Copy 2 : Available
            var copy2 = new BookCopy(bookId, sourceSiteId);
            // Copy 3 : autre site
            var copyOnOtherSite = new BookCopy(bookId, Guid.NewGuid());

            repo.Add(copy1);
            repo.Add(copy2);
            repo.Add(copyOnOtherSite);

            var command = new RequestTransferCommand(bookId, sourceSiteId, targetSiteId);

            // Act
            var result = handler.Handle(command);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(copy1.Id, result.BookCopyId); // le premier trouvé doit être utilisé

            var storedCopy1 = repo.Copies.Single(c => c.Id == copy1.Id);
            var storedCopy2 = repo.Copies.Single(c => c.Id == copy2.Id);
            var storedOther = repo.Copies.Single(c => c.Id == copyOnOtherSite.Id);

            Assert.Equal(BookCopyStatus.InTransfer, storedCopy1.Status);
            Assert.Equal(BookCopyStatus.Available, storedCopy2.Status);
            Assert.Equal(BookCopyStatus.Available, storedOther.Status);
        }

        [Fact]
        public void RequestTransferHandler_Should_Handle_BookId_Not_Matching_Any_Copy()
        {
            // Intention : tester un cas aux bornes où le BookId ne correspond à aucune copy existante.
            // Arrange
            var repo = new FakeBookCopyRepository();
            var handler = new RequestTransferHandler(repo);

            var existingBookId = Guid.NewGuid();
            var otherBookId = Guid.NewGuid();
            var sourceSiteId = Guid.NewGuid();
            var targetSiteId = Guid.NewGuid();

            var copyOfOtherBook = new BookCopy(otherBookId, sourceSiteId);
            repo.Add(copyOfOtherBook);

            var command = new RequestTransferCommand(existingBookId, sourceSiteId, targetSiteId);

            // Act
            var result = handler.Handle(command);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("NO_COPY_AVAILABLE_AT_SOURCE_SITE", result.ErrorCode);
            Assert.Equal(Guid.Empty, result.BookCopyId);
            Assert.Equal(BookCopyStatus.Available, copyOfOtherBook.Status);
        }

        #endregion
    }
}
