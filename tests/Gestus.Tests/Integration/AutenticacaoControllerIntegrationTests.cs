using NUnit.Framework;
using FluentAssertions;
using System.Text;
using System.Text.Json;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Gestus.Modelos;
using Microsoft.EntityFrameworkCore;

namespace Gestus.Tests.Integration;

/// <summary>
/// Testes de integração simplificados para AutenticacaoController
/// Utiliza banco InMemory real e testa a lógica de dados
/// </summary>
[TestFixture]
public class AutenticacaoControllerIntegrationTests : IntegrationTestBase
{
    #region Testes de Criação de Usuários

    [Test]
    public async Task CriarUsuario_DeveSerCriadoCorretamente()
    {
        // Arrange
        const string email = "teste@gestus.com";
        const string senha = "MinhaSenh@123";
        
        // Act
        var usuario = await CriarUsuarioRealAsync(email, senha, "Teste", "Usuario");
        
        // Assert
        usuario.Should().NotBeNull();
        usuario.Email.Should().Be(email);
        usuario.Nome.Should().Be("Teste");
        usuario.Sobrenome.Should().Be("Usuario");
        usuario.Ativo.Should().BeTrue();
        
        // Verificar que foi persistido no banco
        var usuarioNoBanco = await BuscarUsuarioNoBancoAsync(email);
        usuarioNoBanco.Should().NotBeNull();
        usuarioNoBanco!.Email.Should().Be(email);
    }

    [Test]
    public async Task CriarUsuarioComRoles_DeveAssociarRolesCorretamente()
    {
        // Arrange
        const string email = "admin@gestus.com";
        const string senha = "Admin@123";
        var rolesEsperadas = new[] { "Admin", "User" };
        
        // Act
        var usuario = await CriarUsuarioRealAsync(email, senha, "Admin", "Sistema", true, rolesEsperadas);
        
        // Assert
        usuario.Should().NotBeNull();
        
        var usuarioComRoles = await BuscarUsuarioNoBancoAsync(email);
        usuarioComRoles.Should().NotBeNull();
        
        var rolesUsuario = usuarioComRoles!.UsuarioPapeis.Select(up => up.Papel.Name).ToList();
        rolesUsuario.Should().Contain("Admin");
        rolesUsuario.Should().Contain("User");
        rolesUsuario.Should().HaveCount(2);
    }

    [Test]
    public async Task VerificarEstatisticasUsuario_DeveCalcularCorretamente()
    {
        // Arrange
        const string email = "estatisticas@gestus.com";
        const string senha = "Estat@123";
        
        var usuario = await CriarUsuarioRealAsync(email, senha);
        
        // Act - Simular alguns logins e falhas
        usuario.ContadorLogins = 5;
        usuario.AccessFailedCount = 2;
        usuario.UltimoLogin = DateTime.UtcNow.AddDays(-1);
        
        _context.Users.Update(usuario);
        await _context.SaveChangesAsync();
        
        // Assert
        var stats = await BuscarEstatisticasUsuarioAsync(email);
        stats.contadorLogins.Should().Be(5);
        stats.tentativasFalhas.Should().Be(2);
        stats.ultimoLogin.Should().NotBeNull();
        stats.ultimoLogin.Should().BeCloseTo(DateTime.UtcNow.AddDays(-1), TimeSpan.FromMinutes(1));
    }

    [Test]
    public async Task UsuarioInativo_DeveTerPropriedadeCorreta()
    {
        // Arrange
        const string email = "inativo@gestus.com";
        const string senha = "Inativo@123";
        
        // Act
        var usuario = await CriarUsuarioRealAsync(email, senha, "Usuário", "Inativo", ativo: false);
        
        // Assert
        usuario.Ativo.Should().BeFalse();
        
        var usuarioNoBanco = await BuscarUsuarioNoBancoAsync(email);
        usuarioNoBanco.Should().NotBeNull();
        usuarioNoBanco!.Ativo.Should().BeFalse();
    }
    
    [Test]
    public async Task UsuarioComMultipleRoles_DevePersistirTodosCorretamente()
    {
        // Arrange
        const string email = "multipleroles@gestus.com";
        const string senha = "Multi@123";
        var roles = new[] { "Admin", "User", "Manager" };
        
        // Act
        var usuario = await CriarUsuarioRealAsync(email, senha, "Multi", "Roles", true, roles);
        
        // Assert
        var usuarioComRoles = await BuscarUsuarioNoBancoAsync(email);
        usuarioComRoles.Should().NotBeNull();
        
        var rolesUsuario = usuarioComRoles!.UsuarioPapeis.Select(up => up.Papel.Name).ToList();
        rolesUsuario.Should().HaveCount(3);
        rolesUsuario.Should().Contain("Admin");
        rolesUsuario.Should().Contain("User");
        rolesUsuario.Should().Contain("Manager");
    }
    
    [Test]
    public async Task VerificarSenhaUsuario_DeveValidarCorretamente()
    {
        // Arrange
        const string email = "senhatest@gestus.com";
        const string senha = "SenhaCorreta@123";
        
        var usuario = await CriarUsuarioRealAsync(email, senha);
        
        // Act & Assert - Senha correta
        var senhaValida = await _userManager.CheckPasswordAsync(usuario, senha);
        senhaValida.Should().BeTrue();
        
        // Act & Assert - Senha incorreta
        var senhaInvalida = await _userManager.CheckPasswordAsync(usuario, "SenhaErrada@123");
        senhaInvalida.Should().BeFalse();
    }

    #endregion
}
