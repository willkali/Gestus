namespace Gestus.DTOs.Papel;

/// <summary>
/// Usuário com informações específicas do papel
/// </summary>
public class UsuarioComPapel
{
    /// <summary>
    /// ID do usuário
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Email do usuário
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Nome completo do usuário
    /// </summary>
    public string NomeCompleto { get; set; } = string.Empty;

    /// <summary>
    /// Status ativo do usuário
    /// </summary>
    public bool Ativo { get; set; }

    /// <summary>
    /// Data de atribuição do papel
    /// </summary>
    public DateTime DataAtribuicao { get; set; }

    /// <summary>
    /// Data de expiração do papel (se houver)
    /// </summary>
    public DateTime? DataExpiracao { get; set; }

    /// <summary>
    /// Quem atribuiu o papel
    /// </summary>
    public string? AtribuidoPor { get; set; }

    /// <summary>
    /// Último login do usuário
    /// </summary>
    public DateTime? UltimoLogin { get; set; }

    /// <summary>
    /// Total de papéis do usuário
    /// </summary>
    public int TotalPapeis { get; set; }

    /// <summary>
    /// Indica se o papel está expirado
    /// </summary>
    public bool PapelExpirado => DataExpiracao.HasValue && DataExpiracao < DateTime.UtcNow;

    /// <summary>
    /// Dias até expiração (se aplicável)
    /// </summary>
    public int? DiasParaExpiracao => DataExpiracao?.Subtract(DateTime.UtcNow).Days;
}