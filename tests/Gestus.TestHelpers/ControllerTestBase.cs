using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using Moq;
using Gestus.Modelos;
using Gestus.Dados;

namespace Gestus.TestHelpers;

/// <summary>
/// Classe base específica para testes de controllers
/// Fornece mocks e helpers para testar controllers do Gestus
/// </summary>
public class ControllerTestBase : TestBase
{
    protected Mock<UserManager<Usuario>> MockUserManager { get; private set; }
    protected Mock<SignInManager<Usuario>> MockSignInManager { get; private set; }
    protected Mock<RoleManager<Papel>> MockRoleManager { get; private set; }

    public ControllerTestBase()
    {
        SetupMocks();
    }

    private void SetupMocks()
    {
        // Mock UserManager
        MockUserManager = CriarMockUserManager();

        // Mock RoleManager
        var roleStore = new Mock<IRoleStore<Papel>>();
        MockRoleManager = new Mock<RoleManager<Papel>>(
            roleStore.Object, null, null, null, null);

        // Mock SignInManager
        var contextAccessor = new Mock<IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<Usuario>>();
        MockSignInManager = new Mock<SignInManager<Usuario>>(
            MockUserManager.Object,
            contextAccessor.Object,
            claimsFactory.Object,
            null, null, null, null);
    }

    /// <summary>
    /// Cria um HttpContext mockado com usuário autenticado
    /// </summary>
    protected HttpContext CriarHttpContextComUsuario(
        int usuarioId = 1, 
        string email = "teste@gestus.com", 
        string papel = "Usuario",
        params string[] permissoes)
    {
        var claims = CriarClaimsUsuario(usuarioId, email, papel, permissoes);
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext();
        httpContext.User = principal;

        // Adicionar informações de request básicas
        httpContext.Request.Scheme = "http";
        httpContext.Request.Host = new HostString("localhost:5000");

        return httpContext;
    }

    /// <summary>
    /// Configura o ControllerContext de um controller com usuário mockado
    /// </summary>
    protected void ConfigurarControllerContext<T>(
        T controller, 
        int usuarioId = 1, 
        string email = "teste@gestus.com", 
        string papel = "Usuario",
        params string[] permissoes) where T : ControllerBase
    {
        var httpContext = CriarHttpContextComUsuario(usuarioId, email, papel, permissoes);
        
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    /// <summary>
    /// Verifica se o resultado é do tipo esperado e retorna ele
    /// </summary>
    protected T AssertResultType<T>(IActionResult result) where T : IActionResult
    {
        if (result is not T typedResult)
        {
            throw new AssertionException($"Esperado resultado do tipo {typeof(T).Name}, mas recebido {result?.GetType().Name ?? "null"}");
        }
        return typedResult;
    }

    /// <summary>
    /// Extrai dados do resultado OK
    /// </summary>
    protected T ExtrairDadosOkResult<T>(IActionResult result)
    {
        var okResult = AssertResultType<OkObjectResult>(result);
        if (okResult.Value is not T data)
        {
            throw new AssertionException($"Esperado dados do tipo {typeof(T).Name}, mas recebido {okResult.Value?.GetType().Name ?? "null"}");
        }
        return data;
    }

    /// <summary>
    /// Extrai dados do resultado Created
    /// </summary>
    protected T ExtrairDadosCreatedResult<T>(IActionResult result)
    {
        var createdResult = AssertResultType<CreatedAtActionResult>(result);
        if (createdResult.Value is not T data)
        {
            throw new AssertionException($"Esperado dados do tipo {typeof(T).Name}, mas recebido {createdResult.Value?.GetType().Name ?? "null"}");
        }
        return data;
    }

    /// <summary>
    /// Verifica se o resultado é BadRequest e retorna a mensagem de erro
    /// </summary>
    protected string ExtrairMensagemBadRequest(IActionResult result)
    {
        var badRequestResult = AssertResultType<BadRequestObjectResult>(result);
        return badRequestResult.Value?.ToString() ?? "Erro desconhecido";
    }

    /// <summary>
    /// Verifica se o resultado é Unauthorized
    /// </summary>
    protected void AssertUnauthorized(IActionResult result)
    {
        AssertResultType<UnauthorizedResult>(result);
    }

    /// <summary>
    /// Verifica se o resultado é Forbidden
    /// </summary>
    protected void AssertForbidden(IActionResult result)
    {
        if (result is not (ForbidResult or ObjectResult { StatusCode: 403 }))
        {
            throw new AssertionException($"Esperado resultado Forbidden (403), mas recebido {result?.GetType().Name ?? "null"}");
        }
    }

    /// <summary>
    /// Verifica se o resultado é NotFound
    /// </summary>
    protected void AssertNotFound(IActionResult result)
    {
        AssertResultType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// Cria um usuário mockado para testes
    /// </summary>
    protected Usuario CriarUsuarioMock(
        int id = 1,
        string email = "teste@gestus.com",
        string nome = "Usuario",
        string sobrenome = "Teste",
        bool ativo = true)
    {
        return new Usuario
        {
            Id = id,
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
    }

    /// <summary>
    /// Cria um papel mockado para testes
    /// </summary>
    protected Papel CriarPapelMock(
        int id = 1,
        string nome = "TestePapel",
        string descricao = "Papel de teste",
        bool ativo = true)
    {
        return new Papel
        {
            Id = id,
            Name = nome,
            NormalizedName = nome.ToUpper(),
            Descricao = descricao,
            Ativo = ativo,
            DataCriacao = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Exceção personalizada para assertions nos testes
/// </summary>
public class AssertionException : Exception
{
    public AssertionException(string message) : base(message) { }
}