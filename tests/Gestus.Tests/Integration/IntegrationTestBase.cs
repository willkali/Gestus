using NUnit.Framework;
using FluentAssertions;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Gestus.Modelos;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Gestus.Dados;
using System.Net;

namespace Gestus.Tests.Integration;

/// <summary>
/// Classe base para testes de integração simplificada
/// Testa diretamente com banco InMemory e sem HTTP real
/// </summary>
public class IntegrationTestBase
{
    protected GestusDbContexto _context = null!;
    protected UserManager<Usuario> _userManager = null!;
    protected RoleManager<Papel> _roleManager = null!;
    protected IServiceProvider _serviceProvider = null!;

    [SetUp]
    public async Task SetUp()
    {
        // Criar serviços in-memory para testes
        var services = new ServiceCollection();
        
        // Configurar Entity Framework InMemory
        services.AddDbContext<GestusDbContexto>(options =>
            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));
        
        // Configurar Identity
        services.AddIdentity<Usuario, Papel>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 3;
                options.SignIn.RequireConfirmedEmail = false;
            })
            .AddEntityFrameworkStores<GestusDbContexto>()
            .AddDefaultTokenProviders();
            
        // Adicionar outros serviços necessários
        services.AddLogging();
        
        _serviceProvider = services.BuildServiceProvider();
        
        // Obter instâncias dos serviços
        _context = _serviceProvider.GetRequiredService<GestusDbContexto>();
        _userManager = _serviceProvider.GetRequiredService<UserManager<Usuario>>();
        _roleManager = _serviceProvider.GetRequiredService<RoleManager<Papel>>();
        
        // Garantir que o banco está criado
        await _context.Database.EnsureCreatedAsync();
        
        // Seed inicial
        await SeedBasicDataAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        // Limpar banco após cada teste
        if (_context != null)
        {
            await _context.Database.EnsureDeletedAsync();
        }
        
        _serviceProvider?.GetService<IServiceScope>()?.Dispose();
    }

    #region Métodos de Helper para Dados de Teste

    /// <summary>
    /// Cria dados básicos necessários para testes (roles, etc.)
    /// </summary>
    protected virtual async Task SeedBasicDataAsync()
    {
        // Criar roles básicos
        var roles = new[] { "Admin", "User", "Manager" };
        foreach (var roleName in roles)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var role = new Papel { Name = roleName, Ativo = true, DataCriacao = DateTime.UtcNow };
                await _roleManager.CreateAsync(role);
            }
        }

        // Criar algumas permissões básicas
        var permissoes = new[]
        {
            new Permissao { Nome = "usuarios.listar", Descricao = "Listar usuários", Ativo = true },
            new Permissao { Nome = "usuarios.criar", Descricao = "Criar usuários", Ativo = true },
            new Permissao { Nome = "usuarios.editar", Descricao = "Editar usuários", Ativo = true },
            new Permissao { Nome = "usuarios.deletar", Descricao = "Deletar usuários", Ativo = true }
        };

        foreach (var permissao in permissoes)
        {
            if (!_context.Permissoes.Any(p => p.Nome == permissao.Nome))
            {
                _context.Permissoes.Add(permissao);
            }
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Cria um usuário de teste no banco real
    /// </summary>
    protected async Task<Usuario> CriarUsuarioRealAsync(
        string email, 
        string senha, 
        string nome = "Usuário", 
        string sobrenome = "Teste",
        bool ativo = true,
        string[]? roles = null)
    {
        var usuario = new Usuario
        {
            Email = email,
            UserName = email,
            Nome = nome,
            Sobrenome = sobrenome,
            EmailConfirmed = true,
            Ativo = ativo,
            DataCriacao = DateTime.UtcNow,
            ContadorLogins = 0,
            PreferenciaTimezone = "America/Sao_Paulo"
        };

        var resultado = await _userManager.CreateAsync(usuario, senha);
        if (!resultado.Succeeded)
        {
            throw new InvalidOperationException($"Falha ao criar usuário de teste: {string.Join(", ", resultado.Errors.Select(e => e.Description))}");
        }

        // Adicionar roles se especificadas
        if (roles != null && roles.Length > 0)
        {
            await _userManager.AddToRolesAsync(usuario, roles);
        }

        return usuario;
    }


    /// <summary>
    /// Verifica dados no banco após uma operação
    /// </summary>
    protected async Task<Usuario?> BuscarUsuarioNoBancoAsync(string email)
    {
        return await _context.Users
            .Include(u => u.UsuarioPapeis)
                .ThenInclude(up => up.Papel)
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    /// <summary>
    /// Verifica estatísticas do usuário no banco
    /// </summary>
    protected async Task<(int contadorLogins, int tentativasFalhas, DateTime? ultimoLogin)> 
        BuscarEstatisticasUsuarioAsync(string email)
    {
        var usuario = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (usuario == null)
            return (0, 0, null);

        return (usuario.ContadorLogins, usuario.AccessFailedCount, usuario.UltimoLogin);
    }

    #endregion
}