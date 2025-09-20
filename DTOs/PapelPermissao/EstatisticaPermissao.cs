namespace Gestus.DTOs.PapelPermissao;

/// <summary>
/// Estatística por permissão
/// </summary>
public class EstatisticaPermissao
{
    public int PermissaoId { get; set; }
    public string PermissaoNome { get; set; } = string.Empty;
    public string? Categoria { get; set; }
    public int TotalPapeis { get; set; }
}