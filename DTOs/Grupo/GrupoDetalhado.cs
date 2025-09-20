using Gestus.DTOs.Comuns;

namespace Gestus.DTOs.Grupo;

/// <summary>
/// Grupo com informações detalhadas para busca
/// </summary>
public class GrupoDetalhado
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string? Tipo { get; set; }
    public bool Ativo { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
    public int TotalUsuarios { get; set; }
    public int UsuariosAtivos { get; set; }
    public DateTime? UltimaAdesao { get; set; }
    public Dictionary<string, int> DistribuicaoPorPapel { get; set; } = new();
    public decimal? TaxaCrescimento { get; set; }
    public List<string> Tags { get; set; } = new();
}