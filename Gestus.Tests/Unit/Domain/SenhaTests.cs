using Gestus.Domain.ValueObjects;

namespace Gestus.Tests.Unit.Domain;

/// <summary>
/// Testes unitários para o Value Object Senha.
/// </summary>
public class SenhaTests
{
    [Fact]
    public void CriarSenha_SenhaValida_DeveCriarComSucesso()
    {
        // Arrange
        var senhaTexto = "Senh@123";

        // Act
        var senha = new Senha(senhaTexto);

        // Assert
        Assert.NotNull(senha);
        Assert.NotNull(senha.Hash);
        Assert.NotEmpty(senha.Hash);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CriarSenha_SenhaVaziaOuNula_DeveLancarExcecao(string? senhaInvalida)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Senha(senhaInvalida!));
    }

    [Theory]
    [InlineData("abc123")]           // Muito curta (< 8 caracteres)
    [InlineData("abcdefgh")]          // Sem número, sem maiúscula, sem especial
    [InlineData("ABCDEFGH")]          // Sem número, sem minúscula, sem especial
    [InlineData("12345678")]          // Sem letra
    [InlineData("Abcdefgh")]          // Sem número, sem especial
    [InlineData("Abc12345")]          // Sem especial
    [InlineData("ABC@1234")]          // Sem minúscula
    [InlineData("abc@1234")]          // Sem maiúscula
    public void CriarSenha_SenhaNaoAtendeRequisitos_DeveLancarExcecao(string senhaInvalida)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new Senha(senhaInvalida));
        Assert.Contains("não atende aos requisitos de complexidade", exception.Message);
    }

    [Theory]
    [InlineData("Senh@123")]
    [InlineData("P@ssw0rd")]
    [InlineData("Minha$enh@123")]
    [InlineData("C0mpl3x@Pass")]
    public void CriarSenha_SenhaAtendeRequisitos_DeveCriarComSucesso(string senhaValida)
    {
        // Act
        var senha = new Senha(senhaValida);

        // Assert
        Assert.NotNull(senha);
        Assert.NotNull(senha.Hash);
    }

    [Fact]
    public void Verificar_SenhaCorreta_DeveRetornarTrue()
    {
        // Arrange
        var senhaTexto = "Senh@123";
        var senha = new Senha(senhaTexto);

        // Act
        var resultado = senha.Verificar(senhaTexto);

        // Assert
        Assert.True(resultado);
    }

    [Fact]
    public void Verificar_SenhaIncorreta_DeveRetornarFalse()
    {
        // Arrange
        var senhaTexto = "Senh@123";
        var senha = new Senha(senhaTexto);

        // Act
        var resultado = senha.Verificar("Senh@456");

        // Assert
        Assert.False(resultado);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Verificar_SenhaVaziaOuNula_DeveRetornarFalse(string? senhaInvalida)
    {
        // Arrange
        var senha = new Senha("Senh@123");

        // Act
        var resultado = senha.Verificar(senhaInvalida!);

        // Assert
        Assert.False(resultado);
    }

    [Fact]
    public void DeHash_HashValido_DeveCriarSenhaComHash()
    {
        // Arrange
        var hashExistente = "dGVzdGVoYXNoMTIzNDU2Nzg5MA=="; // Hash fictício

        // Act
        var senha = Senha.DeHash(hashExistente);

        // Assert
        Assert.NotNull(senha);
        Assert.Equal(hashExistente, senha.Hash);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void DeHash_HashVazioOuNulo_DeveLancarExcecao(string? hashInvalido)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Senha.DeHash(hashInvalido!));
    }

    [Fact]
    public void Equals_SenhasComMesmoHash_DeveRetornarTrue()
    {
        // Arrange
        var hash = "dGVzdGVoYXNoMTIzNDU2Nzg5MA==";
        var senha1 = Senha.DeHash(hash);
        var senha2 = Senha.DeHash(hash);

        // Act & Assert
        Assert.Equal(senha1, senha2);
        Assert.True(senha1 == senha2);
    }

    [Fact]
    public void Equals_SenhasComHashesDiferentes_DeveRetornarFalse()
    {
        // Arrange
        var senha1 = new Senha("Senh@123");
        var senha2 = new Senha("Senh@456");

        // Act & Assert
        Assert.NotEqual(senha1, senha2);
        Assert.True(senha1 != senha2);
    }

    [Fact]
    public void ToString_DeveRetornarMascara()
    {
        // Arrange
        var senha = new Senha("Senh@123");

        // Act
        var resultado = senha.ToString();

        // Assert
        Assert.Equal("********", resultado);
    }

    [Fact]
    public void GetHashCode_SenhasComMesmoHash_DevemTerMesmoHashCode()
    {
        // Arrange
        var hash = "dGVzdGVoYXNoMTIzNDU2Nzg5MA==";
        var senha1 = Senha.DeHash(hash);
        var senha2 = Senha.DeHash(hash);

        // Act & Assert
        Assert.Equal(senha1.GetHashCode(), senha2.GetHashCode());
    }

    [Fact]
    public void Hash_SenhasIguais_DevemGerarHashesDiferentes()
    {
        // Arrange
        var senhaTexto = "Senh@123";
        var senha1 = new Senha(senhaTexto);
        var senha2 = new Senha(senhaTexto);

        // Act & Assert
        // Hashes devem ser diferentes porque o salt é aleatório
        Assert.NotEqual(senha1.Hash, senha2.Hash);
        
        // Mas ambas devem verificar corretamente
        Assert.True(senha1.Verificar(senhaTexto));
        Assert.True(senha2.Verificar(senhaTexto));
    }
}
