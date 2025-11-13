using System;
using Library.Domain;
using Xunit;

namespace Library.Tests;

public class SiteTests
{
    [Fact]
    public void Can_create_site_with_valid_name()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var site = new Site(id, "Bibliothèque Centrale", "1 rue de la Paix");

        // Assert
        Assert.Equal(id, site.Id);
        Assert.Equal("Bibliothèque Centrale", site.Name);
        Assert.Equal("1 rue de la Paix", site.Address);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Constructor_throws_when_name_is_null_or_whitespace(string? name)
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act + Assert
        Assert.Throws<ArgumentException>(() =>
        {
            _ = new Site(id, name!, "Adresse");
        });
    }
}
