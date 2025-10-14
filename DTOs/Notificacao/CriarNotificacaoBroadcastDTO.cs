using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Notificacao;

/// <summary>
/// DTO para criar múltiplas notificações (broadcast)
/// </summary>
public class CriarNotificacaoBroadcastDTO
{
    [Required(ErrorMessage = "Os usuários são obrigatórios")]
    [MinLength(1, ErrorMessage = "Pelo menos um usuário deve ser especificado")]
    public List<int> UsuarioIds { get; set; } = new();

    [Required(ErrorMessage = "O tipo é obrigatório")]
    [StringLength(50, ErrorMessage = "O tipo deve ter no máximo 50 caracteres")]
    public string Tipo { get; set; } = string.Empty;

    [Required(ErrorMessage = "O título é obrigatório")]
    [StringLength(200, ErrorMessage = "O título deve ter no máximo 200 caracteres")]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "A mensagem é obrigatória")]
    [StringLength(1000, ErrorMessage = "A mensagem deve ter no máximo 1000 caracteres")]
    public string Mensagem { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "O ícone deve ter no máximo 50 caracteres")]
    public string? Icone { get; set; }

    [Required(ErrorMessage = "A cor é obrigatória")]
    [StringLength(20, ErrorMessage = "A cor deve ter no máximo 20 caracteres")]
    public string Cor { get; set; } = "info";

    [StringLength(100, ErrorMessage = "A origem deve ter no máximo 100 caracteres")]
    public string? Origem { get; set; } = "Sistema";

    [Range(1, 3, ErrorMessage = "A prioridade deve ser entre 1 (alta) e 3 (baixa)")]
    public int Prioridade { get; set; } = 2;

    public DateTime? DataExpiracao { get; set; }
    public bool EnviarEmail { get; set; } = false;
    public string? DadosAdicionais { get; set; }
}