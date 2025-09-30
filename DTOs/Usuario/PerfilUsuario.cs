using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Usuario;

/// <summary>
/// Perfil completo do usuário
/// </summary>
public class PerfilUsuario
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Sobrenome { get; set; } = string.Empty;
    public string NomeCompleto { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public string? CaminhoFotoPerfil { get; set; }
    public string? UrlFotoPerfil { get; set; } // URL para download seguro
    public string? Profissao { get; set; }
    public string? Departamento { get; set; }
    public string? Bio { get; set; }
    public DateTime? DataNascimento { get; set; }
    public string? EnderecoCompleto { get; set; }
    public string? Cidade { get; set; }
    public string? Estado { get; set; }
    public string? Cep { get; set; }
    public string? TelefoneAlternativo { get; set; }
    public string PreferenciaIdioma { get; set; } = "pt-BR";
    public string PreferenciaTimezone { get; set; } = "America/Sao_Paulo";
    public ConfiguracaoPrivacidade? Privacidade { get; set; }
    public ConfiguracaoNotificacao? Notificacoes { get; set; }
    public CompletudePerfil? CompletudePerfil { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
    public DateTime? UltimoLogin { get; set; }
}

/// <summary>
/// Informações sobre completude do perfil
/// </summary>
public class CompletudePerfil
{
    /// <summary>
    /// Percentual de completude (0-100)
    /// </summary>
    public double PercentualCompleto { get; set; }

    /// <summary>
    /// Quantidade de campos preenchidos
    /// </summary>
    public int CamposPreenchidos { get; set; }

    /// <summary>
    /// Total de campos possíveis
    /// </summary>
    public int TotalCampos { get; set; }

    /// <summary>
    /// Todos os campos obrigatórios estão completos
    /// </summary>
    public bool CamposObrigatoriosCompletos { get; set; }

    /// <summary>
    /// Nível textual da completude (Excelente, Muito Bom, etc.)
    /// </summary>
    public string NivelCompletude { get; set; } = string.Empty;

    /// <summary>
    /// Cor sugerida para exibição (hex)
    /// </summary>
    public string Cor { get; set; } = string.Empty;

    /// <summary>
    /// Lista detalhada de campos e seu status
    /// </summary>
    public List<CampoCompletude> Campos { get; set; } = new();

    /// <summary>
    /// Sugestões para melhorar o perfil
    /// </summary>
    public List<string> Sugestoes { get; set; } = new();

    /// <summary>
    /// Próximo passo sugerido
    /// </summary>
    public string ProximoPasso { get; set; } = string.Empty;

    /// <summary>
    /// Quando foi calculado
    /// </summary>
    public DateTime DataCalculado { get; set; }
}

/// <summary>
/// Informações sobre um campo específico do perfil
/// </summary>
public class CampoCompletude
{
    public CampoCompletude(string nome, bool preenchido, bool obrigatorio, int peso)
    {
        Nome = nome;
        Preenchido = preenchido;
        Obrigatorio = obrigatorio;
        Peso = peso;
    }

    /// <summary>
    /// Nome do campo
    /// </summary>
    public string Nome { get; set; }

    /// <summary>
    /// Se o campo está preenchido
    /// </summary>
    public bool Preenchido { get; set; }

    /// <summary>
    /// Se o campo é obrigatório
    /// </summary>
    public bool Obrigatorio { get; set; }

    /// <summary>
    /// Peso do campo na pontuação (1-15)
    /// </summary>
    public int Peso { get; set; }
}

/// <summary>
/// Request para atualização de perfil
/// </summary>
public class AtualizarPerfilRequest
{
    [MaxLength(100, ErrorMessage = "Nome deve ter no máximo 100 caracteres")]
    public string? Nome { get; set; }

    [MaxLength(150, ErrorMessage = "Sobrenome deve ter no máximo 150 caracteres")]
    public string? Sobrenome { get; set; }

    [Phone(ErrorMessage = "Telefone deve ter formato válido")]
    [MaxLength(20, ErrorMessage = "Telefone deve ter no máximo 20 caracteres")]
    public string? Telefone { get; set; }

    [MaxLength(100, ErrorMessage = "Profissão deve ter no máximo 100 caracteres")]
    public string? Profissao { get; set; }

    [MaxLength(100, ErrorMessage = "Departamento deve ter no máximo 100 caracteres")]
    public string? Departamento { get; set; }

    [MaxLength(1000, ErrorMessage = "Bio deve ter no máximo 1000 caracteres")]
    public string? Bio { get; set; }

    public DateTime? DataNascimento { get; set; }

    [MaxLength(100, ErrorMessage = "Endereço deve ter no máximo 100 caracteres")]
    public string? EnderecoCompleto { get; set; }

    [MaxLength(50, ErrorMessage = "Cidade deve ter no máximo 50 caracteres")]
    public string? Cidade { get; set; }

    [MaxLength(2, ErrorMessage = "Estado deve ter 2 caracteres")]
    public string? Estado { get; set; }

    [RegularExpression(@"^\d{5}-?\d{3}$", ErrorMessage = "CEP deve ter formato válido")]
    public string? Cep { get; set; }

    [Phone(ErrorMessage = "Telefone alternativo deve ter formato válido")]
    [MaxLength(20, ErrorMessage = "Telefone alternativo deve ter no máximo 20 caracteres")]
    public string? TelefoneAlternativo { get; set; }

    [MaxLength(10, ErrorMessage = "Idioma deve ter no máximo 10 caracteres")]
    public string? PreferenciaIdioma { get; set; }

    [MaxLength(50, ErrorMessage = "Timezone deve ter no máximo 50 caracteres")]
    public string? PreferenciaTimezone { get; set; }
}

/// <summary>
/// Configurações de privacidade
/// </summary>
public class ConfiguracaoPrivacidade
{
    public bool ExibirEmail { get; set; }
    public bool ExibirTelefone { get; set; }
    public bool ExibirDataNascimento { get; set; }
    public bool ExibirEndereco { get; set; }
    public bool PerfilPublico { get; set; }
}

/// <summary>
/// Configurações de notificação
/// </summary>
public class ConfiguracaoNotificacao
{
    public bool NotificacaoEmail { get; set; }
    public bool NotificacaoSms { get; set; }
    public bool NotificacaoPush { get; set; }
}

/// <summary>
/// Request para alteração de senha
/// </summary>
public class AlterarSenhaRequest
{
    [Required(ErrorMessage = "Senha atual é obrigatória")]
    public string SenhaAtual { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nova senha é obrigatória")]
    [MinLength(6, ErrorMessage = "Nova senha deve ter pelo menos 6 caracteres")]
    public string NovaSenha { get; set; } = string.Empty;

    [Compare("NovaSenha", ErrorMessage = "Confirmação de senha não confere")]
    public string ConfirmarNovaSenha { get; set; } = string.Empty;
}

/// <summary>
/// Request para recuperação de senha
/// </summary>
public class RecuperarSenhaRequest
{
    [Required(ErrorMessage = "Email é obrigatório")]
    [EmailAddress(ErrorMessage = "Email deve ter formato válido")]
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Request para redefinir senha
/// </summary>
public class RedefinirSenhaRequest
{
    [Required(ErrorMessage = "Email é obrigatório")]
    [EmailAddress(ErrorMessage = "Email deve ter formato válido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Token é obrigatório")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nova senha é obrigatória")]
    [MinLength(6, ErrorMessage = "Nova senha deve ter pelo menos 6 caracteres")]
    public string NovaSenha { get; set; } = string.Empty;

    [Compare("NovaSenha", ErrorMessage = "Confirmação de senha não confere")]
    public string ConfirmarNovaSenha { get; set; } = string.Empty;
}