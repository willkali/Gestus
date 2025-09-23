using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Usuario;

/// <summary>
/// Request para criação de usuário
/// </summary>
public class CriarUsuarioRequest
{
    [Required(ErrorMessage = "Email é obrigatório")]
    [EmailAddress(ErrorMessage = "Email deve ter formato válido")]
    [MaxLength(256, ErrorMessage = "Email deve ter no máximo 256 caracteres")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nome é obrigatório")]
    [MaxLength(100, ErrorMessage = "Nome deve ter no máximo 100 caracteres")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Sobrenome é obrigatório")]
    [MaxLength(150, ErrorMessage = "Sobrenome deve ter no máximo 150 caracteres")]
    public string Sobrenome { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Telefone deve ter formato válido")]
    [MaxLength(20, ErrorMessage = "Telefone deve ter no máximo 20 caracteres")]
    public string? Telefone { get; set; }

    [Required(ErrorMessage = "Senha é obrigatória")]
    [MinLength(6, ErrorMessage = "Senha deve ter pelo menos 6 caracteres")]
    public string Senha { get; set; } = string.Empty;

    [Compare("Senha", ErrorMessage = "Confirmação de senha não confere")]
    public string ConfirmarSenha { get; set; } = string.Empty;

    public bool Ativo { get; set; } = true;
    public bool ConfirmarEmailImediatamente { get; set; } = false;
    public bool ConfirmarTelefoneImediatamente { get; set; } = false;

    [MaxLength(500, ErrorMessage = "Observações devem ter no máximo 500 caracteres")]
    public string? Observacoes { get; set; }

    /// <summary>
    /// Lista de papéis para atribuir ao usuário
    /// </summary>
    public List<string>? Papeis { get; set; }

    /// <summary>
    /// Lista de IDs das aplicações para conceder acesso
    /// </summary>
    public List<int>? AplicacoesIds { get; set; }
    
    /// <summary>
    /// Se deve aprovar automaticamente o acesso às aplicações
    /// </summary>
    public bool AprovarAplicacoesAutomaticamente { get; set; } = false;
    
    /// <summary>
    /// Justificativa para acesso às aplicações
    /// </summary>
    [MaxLength(500, ErrorMessage = "Justificativa deve ter no máximo 500 caracteres")]
    public string? JustificativaAplicacoes { get; set; }
    
    /// <summary>
    /// Data de expiração padrão para acessos às aplicações
    /// </summary>
    public DateTime? DataExpiracaoAplicacoes { get; set; }
}