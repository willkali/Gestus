using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Usuario;

/// <summary>
/// Dados de usuário para operações em lote
/// </summary>
public class DadosUsuarioLote
{
    /// <summary>
    /// ID do usuário (para atualizações)
    /// </summary>
    public int? Id { get; set; }

    [EmailAddress(ErrorMessage = "Email deve ter formato válido")]
    [MaxLength(256, ErrorMessage = "Email deve ter no máximo 256 caracteres")]
    public string? Email { get; set; }

    [MaxLength(100, ErrorMessage = "Nome deve ter no máximo 100 caracteres")]
    public string? Nome { get; set; }

    [MaxLength(150, ErrorMessage = "Sobrenome deve ter no máximo 150 caracteres")]
    public string? Sobrenome { get; set; }

    [Phone(ErrorMessage = "Telefone deve ter formato válido")]
    [MaxLength(20, ErrorMessage = "Telefone deve ter no máximo 20 caracteres")]
    public string? Telefone { get; set; }

    /// <summary>
    /// Senha (para criação de usuários)
    /// </summary>
    [MinLength(6, ErrorMessage = "Senha deve ter pelo menos 6 caracteres")]
    public string? Senha { get; set; }

    public bool? Ativo { get; set; }
    public bool? EmailConfirmado { get; set; }
    public bool? TelefoneConfirmado { get; set; }

    [MaxLength(500, ErrorMessage = "Observações devem ter no máximo 500 caracteres")]
    public string? Observacoes { get; set; }

    /// <summary>
    /// Lista de papéis para atribuir
    /// </summary>
    public List<string>? Papeis { get; set; }
}