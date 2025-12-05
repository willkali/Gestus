using Gestus.Domain.ValueObjects;

namespace Gestus.Tests.Unit.Domain;

/// <summary>
/// Testes unitários para o Value Object Email.
/// </summary>
public class EmailTests
{
    [Fact]
    public void CriarEmail_EmailValido_DeveCriarComSucesso()
    {
        // Arrange
        var emailTexto = "usuario@exemplo.com";

        // Act
        var email = new Email(emailTexto);

        // Assert
        Assert.NotNull(email);
        Assert.Equal("usuario@exemplo.com", email.Valor);
    }

    [Fact]
    public void CriarEmail_EmailComMaiusculas_DeveNormalizarParaLowercase()
    {
        // Arrange
        var emailTexto = "USUARIO@EXEMPLO.COM";

        // Act
        var email = new Email(emailTexto);

        // Assert
        Assert.Equal("usuario@exemplo.com", email.Valor);
    }

    [Fact]
    public void CriarEmail_EmailComEspacos_DeveRemoverEspacos()
    {
        // Arrange
        var emailTexto = "  usuario@exemplo.com  ";

        // Act
        var email = new Email(emailTexto);

        // Assert
        Assert.Equal("usuario@exemplo.com", email.Valor);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CriarEmail_EmailVazioOuNulo_DeveLancarExcecao(string? emailInvalido)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Email(emailInvalido!));
    }

    [Theory]
    [InlineData("emailsemarroba.com")]
    [InlineData("@exemplo.com")]
    [InlineData("usuario@")]
    [InlineData("usuario@@exemplo.com")]
    [InlineData("usuario@exemplo")]
    public void CriarEmail_EmailInvalido_DeveLancarExcecao(string emailInvalido)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new Email(emailInvalido));
        Assert.Contains("Email inválido", exception.Message);
    }

    [Fact]
    public void Equals_EmailsIguais_DeveRetornarTrue()
    {
        // Arrange
        var email1 = new Email("usuario@exemplo.com");
        var email2 = new Email("USUARIO@EXEMPLO.COM"); // Diferente case

        // Act & Assert
        Assert.Equal(email1, email2);
        Assert.True(email1 == email2);
    }

    [Fact]
    public void Equals_EmailsDiferentes_DeveRetornarFalse()
    {
        // Arrange
        var email1 = new Email("usuario1@exemplo.com");
        var email2 = new Email("usuario2@exemplo.com");

        // Act & Assert
        Assert.NotEqual(email1, email2);
        Assert.True(email1 != email2);
    }

    [Fact]
    public void ToString_DeveRetornarValorDoEmail()
    {
        // Arrange
        var email = new Email("usuario@exemplo.com");

        // Act
        var resultado = email.ToString();

        // Assert
        Assert.Equal("usuario@exemplo.com", resultado);
    }

    [Fact]
    public void ConversaoImplicita_DeveConverterParaString()
    {
        // Arrange
        var email = new Email("usuario@exemplo.com");

        // Act
        string emailString = email;

        // Assert
        Assert.Equal("usuario@exemplo.com", emailString);
    }

    [Fact]
    public void GetHashCode_EmailsIguais_DevemTerMesmoHashCode()
    {
        // Arrange
        var email1 = new Email("usuario@exemplo.com");
        var email2 = new Email("USUARIO@EXEMPLO.COM");

        // Act & Assert
        Assert.Equal(email1.GetHashCode(), email2.GetHashCode());
    }
}
