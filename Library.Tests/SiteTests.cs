using System;
using Library.Domain;
using Library.Domain.Entities;
using Xunit;

namespace Library.Tests.Domain;

/// <summary>
/// Tests métier pour l’entité Site.
/// S’assure :
/// - du respect des invariants (nom obligatoire),
/// - du nettoyage des données (Trim),
/// - de la gestion correcte de l’adresse,
/// - de la stabilité de l’identité.
/// </summary>
public sealed class SiteTests
{
    // ------------------------------------------------------------
    // 1) Invariants – le nom est obligatoire
    // ------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_Should_Throw_When_Name_Invalid(string? name)
    {
        Assert.Throws<ArgumentException>(() =>
            new Site(Guid.NewGuid(), name!));
    }

    // ------------------------------------------------------------
    // 2) Trimming des espaces
    // ------------------------------------------------------------

    [Fact]
    public void Ctor_Should_Trim_Name()
    {
        var site = new Site(
            Guid.NewGuid(),
            "  Bibliothèque Centrale  "
        );

        Assert.Equal("Bibliothèque Centrale", site.Name);
    }

    // ------------------------------------------------------------
    // 3) L’adresse est optionnelle
    // ------------------------------------------------------------

    [Fact]
    public void Ctor_Should_Accept_Null_Address()
    {
        var site = new Site(Guid.NewGuid(), "Site", null);

        Assert.Equal("Site", site.Name);
        Assert.Null(site.Address);
    }

    [Fact]
    public void Ctor_Should_Set_Address_When_Provided()
    {
        var site = new Site(
            Guid.NewGuid(),
            "Campus",
            "42 Rue des Sciences"
        );

        Assert.Equal("Campus", site.Name);
        Assert.Equal("42 Rue des Sciences", site.Address);
    }

    // ------------------------------------------------------------
    // 4) Identité
    // ------------------------------------------------------------

    [Fact]
    public void Ctor_Should_Set_Id_Exactly_As_Provided()
    {
        var id = Guid.NewGuid();

        var site = new Site(id, "Médiathèque");

        Assert.Equal(id, site.Id);
    }
}
