using System;
using Library.Domain.Entities;
using Library.Domain.Results;
using Xunit;

namespace Library.Tests.Domain;

/// <summary>
/// Tests métier pour BorrowResult.
/// Garantit la cohérence entre les états Success/Fail,
/// la présence ou absence du Loan,
/// et la validité du code d’erreur.
/// </summary>
public sealed class BorrowResultTests
{
    // ------------------------------------------------------------
    // 1) Cas Success : comportement attendu
    // ------------------------------------------------------------

    [Fact]
    public void Ok_Should_Create_Success_Result_With_Loan()
    {
        // Arrange
        var loan = new Loan(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(7));

        // Act
        var result = BorrowResult.Ok(loan);

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.ErrorCode);
        Assert.NotNull(result.Loan);
        Assert.Equal(loan, result.Loan);
    }

    // ------------------------------------------------------------
    // 2) Cas Failure : comportement attendu
    // ------------------------------------------------------------

    [Fact]
    public void Fail_Should_Create_Failure_Result_With_ErrorCode()
    {
        // Act
        var result = BorrowResult.Fail("COPY_NOT_AVAILABLE");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("COPY_NOT_AVAILABLE", result.ErrorCode);
        Assert.Null(result.Loan);
    }

    // ------------------------------------------------------------
    // 3) Cas limites – code d’erreur vide
    // ------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Fail_Should_Allow_Empty_Or_Null_ErrorCode(string? code)
    {
        // Act
        var result = BorrowResult.Fail(code);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(code, result.ErrorCode);
        Assert.Null(result.Loan);
    }

    // ------------------------------------------------------------
    // 4) Vérifie la séparation stricte des états
    // ------------------------------------------------------------

    [Fact]
    public void Success_Result_Should_Never_Have_ErrorCode()
    {
        var loan = new Loan(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(14));

        var result = BorrowResult.Ok(loan);

        Assert.True(result.Success);
        Assert.Null(result.ErrorCode);
    }

    [Fact]
    public void Failure_Result_Should_Never_Have_Loan()
    {
        var result = BorrowResult.Fail("ERROR");

        Assert.False(result.Success);
        Assert.Null(result.Loan);
    }
}
