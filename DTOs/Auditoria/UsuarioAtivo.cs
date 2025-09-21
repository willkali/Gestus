namespace Gestus.DTOs.Auditoria;

/// <summary>
/// Informações de usuário ativo na auditoria
/// </summary>
public class UsuarioAtivo
{
    public int UsuarioId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int TotalOperacoes { get; set; }
    public DateTime? UltimaOperacao { get; set; }
    public Dictionary<string, int> OperacoesPorTipo { get; set; } = new();
}