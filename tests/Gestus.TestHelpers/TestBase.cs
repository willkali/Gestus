using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Moq;
using Gestus.Dados;
using Gestus.Modelos;

namespace Gestus.TestHelpers;

/// <summary>
/// Classe base para testes com helpers comuns
/// Fornece métodos para criar contextos, usuários e mocks
/// </summary>
public class TestBase
{
    /// <summary>
    /// Cria um contexto EF em memória para testes
    /// </summary>
    protected GestusDbContexto CriarContextoMemoria(string? nomeBanco = null)
    {
        nomeBanco ??= Guid.NewGuid().ToString();
        
        var options = new DbContextOptionsBuilder<GestusDbContexto>()
            .UseInMemoryDatabase(databaseName: nomeBanco)
            .EnableSensitiveDataLogging()
            .Options;

        var contexto = new GestusDbContexto(options);
        
        // Garantir que o banco foi criado
        contexto.Database.EnsureCreated();
        
        return contexto;
    }

    /// <summary>
    /// Cria um usuário básico para testes
    /// </summary>
    protected async Task<Usuario> CriarUsuarioTeste(
        GestusDbContexto context, 
        string email = "teste@gestus.com",
        string nome = "Usuario",
        string sobrenome = "Teste",
        bool ativo = true)
    {
        var usuario = new Usuario
        {
            Email = email,
            UserName = email,
            Nome = nome,
            Sobrenome = sobrenome,
            NomeCompleto = $"{nome} {sobrenome}",
            EmailConfirmed = true,
            Ativo = ativo,
            DataCriacao = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid().ToString()
        };

        context.Users.Add(usuario);
        await context.SaveChangesAsync();
        
        return usuario;
    }

    /// <summary>
    /// Cria claims para um usuário (mistura português/inglês como no sistema)
    /// </summary>
    protected List<Claim> CriarClaimsUsuario(
        int usuarioId = 1,
        string email = "teste@gestus.com",
        string papel = "Usuario", 
        params string[] permissoes)
    {
        var claims = new List<Claim>
        {
            new("sub", usuarioId.ToString()),
            new("email", email),
            new("papel", papel), // português como no seu sistema
            new(ClaimTypes.NameIdentifier, usuarioId.ToString()),
            new(ClaimTypes.Email, email)
        };

        // Adicionar permissões individuais
        foreach (var permissao in permissoes)
        {
            claims.Add(new("permissao", permissao)); // português como no seu sistema
        }

        // SuperAdmin tem bypass com "*"
        if (papel == "SuperAdmin")
        {
            claims.Add(new("permissao", "*"));
        }

        return claims;
    }

    /// <summary>
    /// Cria um ClaimsPrincipal para testes de autorização
    /// </summary>
    protected ClaimsPrincipal CriarUsuarioPrincipal(
        int usuarioId = 1,
        string email = "teste@gestus.com", 
        string papel = "Usuario",
        params string[] permissoes)
    {
        var claims = CriarClaimsUsuario(usuarioId, email, papel, permissoes);
        var identity = new ClaimsIdentity(claims, "Test");
        return new ClaimsPrincipal(identity);
    }

    /// <summary>
    /// Cria um papel básico para testes
    /// </summary>
    protected async Task<Papel> CriarPapelTeste(
        GestusDbContexto context,
        string nome = "TestePapel",
        string descricao = "Papel de teste",
        bool ativo = true)
    {
        var papel = new Papel
        {
            Name = nome,
            NormalizedName = nome.ToUpper(),
            Descricao = descricao,
            Ativo = ativo,
            DataCriacao = DateTime.UtcNow
        };

        context.Papeis.Add(papel);
        await context.SaveChangesAsync();
        
        return papel;
    }

    /// <summary>
    /// Cria uma permissão básica para testes
    /// </summary>
    protected async Task<Permissao> CriarPermissaoTeste(
        GestusDbContexto context,
        string nome = "Teste.Acao",
        string recurso = "Teste",
        string acao = "Acao",
        bool ativo = true)
    {
        var permissao = new Permissao
        {
            Nome = nome,
            Descricao = $"Permissão de teste: {nome}",
            Recurso = recurso,
            Acao = acao,
            Ativo = ativo,
            DataCriacao = DateTime.UtcNow
        };

        context.Permissoes.Add(permissao);
        await context.SaveChangesAsync();
        
        return permissao;
    }

    /// <summary>
    /// Mock básico do UserManager para testes
    /// </summary>
    protected Mock<UserManager<Usuario>> CriarMockUserManager()
    {
        var store = new Mock<IUserStore<Usuario>>();
        var mockUserManager = new Mock<UserManager<Usuario>>(
            store.Object,
            null, null, null, null, null, null, null, null);

        return mockUserManager;
    }

    /// <summary>
    /// Mock básico do Logger para testes
    /// </summary>
    protected Mock<ILogger<T>> CriarMockLogger<T>()
    {
        return new Mock<ILogger<T>>();
    }
}