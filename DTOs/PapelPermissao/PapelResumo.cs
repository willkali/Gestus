namespace Gestus.DTOs.PapelPermissao;

/// <summary>
/// Resumo de papel para relatórios de papel-permissão
/// </summary>
public class PapelResumoRelatorio
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string? Categoria { get; set; }
    public int Nivel { get; set; }
    public bool Ativo { get; set; }
}