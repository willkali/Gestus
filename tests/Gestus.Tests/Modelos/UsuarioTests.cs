using NUnit.Framework;
using Gestus.TestHelpers;
using Gestus.Modelos;

namespace Gestus.Tests.Modelos;

/// <summary>
/// Testes básicos do modelo Usuario
/// Primeiro teste para validar a estrutura
/// </summary>
[TestFixture]
public class UsuarioTests : TestBase
{
    [Test]
    public async Task CriarUsuario_ComDadosValidos_DeveFuncionar()
    {
        // Arrange
        using var context = CriarContextoMemoria();
        
        // Act
        var usuario = await CriarUsuarioTeste(context, 
            email: "willian.cavalcante@teste.com",
            nome: "Willian", 
            sobrenome: "Cavalcante");
        
        // Assert
        Assert.That(usuario, Is.Not.Null);
        Assert.That(usuario.Email, Is.EqualTo("willian.cavalcante@teste.com"));
        Assert.That(usuario.Nome, Is.EqualTo("Willian"));
        Assert.That(usuario.Sobrenome, Is.EqualTo("Cavalcante"));
        Assert.That(usuario.NomeCompleto, Is.EqualTo("Willian Cavalcante"));
        Assert.That(usuario.Ativo, Is.True);
        Assert.That(usuario.EmailConfirmed, Is.True);
    }

    [Test]
    public void Usuario_PropriedadesBasicas_DevemFuncionar()
    {
        // Arrange & Act
        var usuario = new Usuario
        {
            Nome = "João",
            Sobrenome = "Silva",
            Email = "joao@teste.com",
            Ativo = true
        };

        // Assert
        Assert.That(usuario.Nome, Is.EqualTo("João"));
        Assert.That(usuario.Sobrenome, Is.EqualTo("Silva"));
        Assert.That(usuario.Email, Is.EqualTo("joao@teste.com"));
        Assert.That(usuario.Ativo, Is.True);
    }

    [Test]
    public void Usuario_ConfiguracoesPadrao_DevemEstarCorretas()
    {
        // Arrange & Act
        var usuario = new Usuario();

        // Assert - Verificar valores padrão
        Assert.That(usuario.Ativo, Is.True, "Usuário deve estar ativo por padrão");
        Assert.That(usuario.ContadorLogins, Is.EqualTo(0), "Contador de logins deve começar em 0");
        Assert.That(usuario.PreferenciaIdioma, Is.EqualTo("pt-BR"), "Idioma padrão deve ser pt-BR");
        Assert.That(usuario.PreferenciaTimezone, Is.EqualTo("America/Sao_Paulo"), "Timezone padrão deve ser America/Sao_Paulo");
        Assert.That(usuario.NotificacaoEmail, Is.True, "Notificação por email deve estar ativa por padrão");
        Assert.That(usuario.NotificacaoPush, Is.True, "Notificação push deve estar ativa por padrão");
        Assert.That(usuario.NotificacaoSms, Is.False, "Notificação SMS deve estar inativa por padrão");
    }

    [Test]
    public async Task Context_DevePermitirSalvarUsuario()
    {
        // Arrange
        using var context = CriarContextoMemoria();
        
        var usuario = new Usuario
        {
            Email = "context@teste.com",
            UserName = "context@teste.com",
            Nome = "Teste",
            Sobrenome = "Context",
            SecurityStamp = Guid.NewGuid().ToString()
        };

        // Act
        context.Users.Add(usuario);
        var resultado = await context.SaveChangesAsync();

        // Assert
        Assert.That(resultado, Is.EqualTo(1), "Deve salvar exatamente 1 registro");
        
        var usuarioSalvo = await context.Users.FindAsync(usuario.Id);
        Assert.That(usuarioSalvo, Is.Not.Null);
        Assert.That(usuarioSalvo.Email, Is.EqualTo("context@teste.com"));
    }
}