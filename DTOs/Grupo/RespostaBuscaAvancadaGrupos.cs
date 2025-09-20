using Gestus.DTOs.Comuns;

namespace Gestus.DTOs.Grupo;

/// <summary>
/// Resposta da busca avançada de grupos
/// </summary>
public class RespostaBuscaAvancadaGrupos : RespostaPaginada<GrupoDetalhado>
{
    /// <summary>
    /// Grupos encontrados
    /// </summary>
    public List<GrupoDetalhado> Grupos { get; set; } = new();

    /// <summary>
    /// Sugestões de busca (quando poucos resultados)
    /// </summary>
    public List<string> Sugestoes { get; set; } = new();

    /// <summary>
    /// Resumo dos critérios aplicados
    /// </summary>
    public List<string> CriteriosAplicados { get; set; } = new();

    /// <summary>
    /// Tempo de execução da busca
    /// </summary>
    public TimeSpan TempoExecucao { get; set; }

    /// <summary>
    /// Estatísticas da busca
    /// </summary>
    public EstatisticasBusca Estatisticas { get; set; } = new();
}