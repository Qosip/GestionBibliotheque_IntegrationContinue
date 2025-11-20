using System;
using System.Collections.Generic;
using System.Linq;
using Library.Application.Commands;
using Library.Application.Handlers;
using Library.Application.Repositories;
using Library.Application.Results;
using Library.Domain.Entities;
using Xunit;

namespace Library.Tests.Application
{
    public sealed class ApplicationSiteTests
    {
        #region Fakes

        private sealed class FakeSiteRepository : ISiteRepository
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

        #endregion

        #region CreateSiteResult

        [Fact]
        public void CreateSiteResult_Ok_Should_SetSuccess_SiteId_AndNullErrorCode()
        {
            // Intention : vérifier que le factory Ok positionne correctement les propriétés en cas de succès.
            // Arrange
            var siteId = Guid.NewGuid();

            // Act
            var result = CreateSiteResult.Ok(siteId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(siteId, result.SiteId);
            Assert.Null(result.ErrorCode);
        }

        [Fact]
        public void CreateSiteResult_Fail_Should_SetFailure_EmptySiteId_AndErrorCode()
        {
            // Intention : vérifier que le factory Fail renseigne l’erreur et remet l’Id à Guid.Empty.
            // Arrange
            const string errorCode = "ANY_ERROR";

            // Act
            var result = CreateSiteResult.Fail(errorCode);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(Guid.Empty, result.SiteId);
            Assert.Equal(errorCode, result.ErrorCode);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CreateSiteResult_Fail_Should_Tolerate_NullOrWhitespace_ErrorCode(string? errorCode)
        {
            // Intention : vérifier que Fail ne jette pas même avec un code d’erreur non renseigné.
            // Arrange
            // Act
            var result = CreateSiteResult.Fail(errorCode!);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(Guid.Empty, result.SiteId);
            Assert.Equal(errorCode, result.ErrorCode);
        }

        #endregion

        #region CreateSiteCommand

        [Fact]
        public void CreateSiteCommand_ParameterlessCtor_Should_Default_Name_ToEmpty_And_Address_ToNull()
        {
            // Intention : vérifier les valeurs par défaut du constructeur sans paramètre.
            // Arrange
            // Act
            var command = new CreateSiteCommand();

            // Assert
            Assert.Equal(string.Empty, command.Name);
            Assert.Null(command.Address);
        }

        [Fact]
        public void CreateSiteCommand_ParameterizedCtor_Should_Set_Properties()
        {
            // Intention : vérifier que le constructeur paramétré recopie correctement les valeurs.
            // Arrange
            const string name = "Site A";
            const string address = "42 rue de la Bibliothèque";

            // Act
            var command = new CreateSiteCommand(name, address);

            // Assert
            Assert.Equal(name, command.Name);
            Assert.Equal(address, command.Address);
        }

        [Fact]
        public void CreateSiteCommand_Properties_Should_Be_Mutable()
        {
            // Intention : vérifier que les propriétés restent modifiables après construction.
            // Arrange
            var command = new CreateSiteCommand();

            // Act
            command.Name = "Site B";
            command.Address = "Adresse B";

            // Assert
            Assert.Equal("Site B", command.Name);
            Assert.Equal("Adresse B", command.Address);
        }

        #endregion

        #region CreateSiteHandler – construction

        [Fact]
        public void CreateSiteHandler_Ctor_Should_Throw_When_Repository_Is_Null()
        {
            // Intention : garantir que le handler refuse une dépendance null explicite.
            // Arrange
            ISiteRepository? repo = null;

            // Act
            var exception = Assert.Throws<ArgumentNullException>(() => new CreateSiteHandler(repo!));

            // Assert
            Assert.Equal("siteRepository", exception.ParamName);
        }

        #endregion

        #region CreateSiteHandler – validation

        [Fact]
        public void CreateSiteHandler_Handle_Should_Throw_When_Command_Is_Null()
        {
            // Intention : s’assurer qu’un appel avec une commande null est rejeté clairement.
            // Arrange
            var repo = new FakeSiteRepository();
            var handler = new CreateSiteHandler(repo);

            // Act
            var exception = Assert.Throws<ArgumentNullException>(() => handler.Handle(null!));

            // Assert
            Assert.Equal("command", exception.ParamName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CreateSiteHandler_Should_ReturnFail_And_NotPersist_When_Name_Invalid(string? name)
        {
            // Intention : vérifier que les noms invalides produisent un échec sans écriture en base.
            // Arrange
            var repo = new FakeSiteRepository();
            var handler = new CreateSiteHandler(repo);

            var command = new CreateSiteCommand
            {
                Name = name!,
                Address = "Adresse quelconque"
            };

            // Act
            var result = handler.Handle(command);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(Guid.Empty, result.SiteId);
            Assert.Equal("INVALID_SITE_NAME", result.ErrorCode);
            Assert.Empty(repo.Sites);
        }

        [Fact]
        public void CreateSiteHandler_Should_Treat_DefaultCommand_As_Invalid()
        {
            // Intention : vérifier que le constructeur par défaut (Name = string.Empty) est refusé.
            // Arrange
            var repo = new FakeSiteRepository();
            var handler = new CreateSiteHandler(repo);
            var command = new CreateSiteCommand(); // Name = ""

            // Act
            var result = handler.Handle(command);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(Guid.Empty, result.SiteId);
            Assert.Equal("INVALID_SITE_NAME", result.ErrorCode);
            Assert.Empty(repo.Sites);
        }

        #endregion

        #region CreateSiteHandler – cas nominal

        [Fact]
        public void CreateSiteHandler_Should_PersistSite_And_ReturnOk_When_Data_Valid_With_Address()
        {
            // Intention : valider le scénario nominal avec nom valide et adresse fournie.
            // Arrange
            var repo = new FakeSiteRepository();
            var handler = new CreateSiteHandler(repo);

            const string name = "Médiathèque Centrale";
            const string address = "10 avenue des Livres";

            var command = new CreateSiteCommand(name, address);

            // Act
            var result = handler.Handle(command);

            // Assert
            Assert.True(result.Success);
            Assert.NotEqual(Guid.Empty, result.SiteId);
            Assert.Null(result.ErrorCode);

            Assert.Single(repo.Sites);
            var stored = repo.Sites[0];

            Assert.Equal(result.SiteId, stored.Id);
            Assert.Equal(name.Trim(), stored.Name);
            Assert.Equal(address, stored.Address);
        }

        [Fact]
        public void CreateSiteHandler_Should_PersistSite_And_ReturnOk_When_Address_Is_Null()
        {
            // Intention : s’assurer qu’un site sans adresse explicite est accepté.
            // Arrange
            var repo = new FakeSiteRepository();
            var handler = new CreateSiteHandler(repo);

            const string name = "Site Sans Adresse";

            var command = new CreateSiteCommand(name, null);

            // Act
            var result = handler.Handle(command);

            // Assert
            Assert.True(result.Success);
            Assert.NotEqual(Guid.Empty, result.SiteId);
            Assert.Null(result.ErrorCode);

            Assert.Single(repo.Sites);
            var stored = repo.Sites[0];

            Assert.Equal(name.Trim(), stored.Name);
            Assert.Null(stored.Address);
        }

        #endregion

        #region CreateSiteHandler – cas improbables / bornes

        [Fact]
        public void CreateSiteHandler_Should_Handle_VeryLongName_And_Address_Without_Exception()
        {
            // Intention : tester la robustesse face à des chaînes anormalement longues.
            // Arrange
            var repo = new FakeSiteRepository();
            var handler = new CreateSiteHandler(repo);

            var veryLongName = new string('N', 10_000);
            var veryLongAddress = new string('A', 20_000);

            var command = new CreateSiteCommand(veryLongName, veryLongAddress);

            // Act
            var exception = Record.Exception(() => handler.Handle(command));

            // Assert
            Assert.Null(exception);
            Assert.Single(repo.Sites);
            var stored = repo.Sites[0];

            Assert.Equal(veryLongName.Trim(), stored.Name);
            Assert.Equal(veryLongAddress, stored.Address);
        }

        [Fact]
        public void CreateSiteHandler_Should_Handle_Name_With_ExtraWhitespace_And_SpecialCharacters()
        {
            // Intention : vérifier le comportement avec un nom contenant espaces et caractères atypiques.
            // Arrange
            var repo = new FakeSiteRepository();
            var handler = new CreateSiteHandler(repo);

            const string rawName = "   Bibliothèque ✨ Centrale   ";
            const string address = "Adresse test";

            var command = new CreateSiteCommand(rawName, address);

            // Act
            var result = handler.Handle(command);

            // Assert
            Assert.True(result.Success);
            Assert.Single(repo.Sites);

            var stored = repo.Sites[0];

            Assert.False(string.IsNullOrWhiteSpace(stored.Name));
            Assert.Equal(rawName.Trim(), stored.Name);
            Assert.Equal(address, stored.Address);
        }

        [Fact]
        public void CreateSiteHandler_Should_Allow_DuplicateNames_With_DifferentIds()
        {
            // Intention : simuler un scénario où plusieurs sites partagent le même nom.
            // Arrange
            var repo = new FakeSiteRepository();
            var handler = new CreateSiteHandler(repo);

            const string name = "Site Dupliqué";
            const string address1 = "Adresse 1";
            const string address2 = "Adresse 2";

            var command1 = new CreateSiteCommand(name, address1);
            var command2 = new CreateSiteCommand(name, address2);

            // Act
            var result1 = handler.Handle(command1);
            var result2 = handler.Handle(command2);

            // Assert
            Assert.True(result1.Success);
            Assert.True(result2.Success);
            Assert.NotEqual(Guid.Empty, result1.SiteId);
            Assert.NotEqual(Guid.Empty, result2.SiteId);
            Assert.NotEqual(result1.SiteId, result2.SiteId);

            Assert.Equal(2, repo.Sites.Count);
            Assert.All(repo.Sites, s => Assert.Equal(name.Trim(), s.Name));
            Assert.Contains(repo.Sites, s => s.Address == address1);
            Assert.Contains(repo.Sites, s => s.Address == address2);
        }

        [Fact]
        public void CreateSiteHandler_Should_Accept_Address_With_Whitespace_Only_And_Preserve_AsIs()
        {
            // Intention : vérifier un cas limite où l’adresse est renseignée mais peu exploitable.
            // Arrange
            var repo = new FakeSiteRepository();
            var handler = new CreateSiteHandler(repo);

            const string name = "Site Adresse Vide";
            const string address = "   ";

            var command = new CreateSiteCommand(name, address);

            // Act
            var result = handler.Handle(command);

            // Assert
            Assert.True(result.Success);
            Assert.Single(repo.Sites);

            var stored = repo.Sites[0];
            Assert.Equal(name.Trim(), stored.Name);
            Assert.Equal(address, stored.Address); // pas de normalisation sur l’adresse dans le domaine
        }

        #endregion
    }
}
