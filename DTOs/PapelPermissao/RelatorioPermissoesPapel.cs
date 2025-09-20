using Gestus.DTOs.Comuns;

namespace Gestus.DTOs.PapelPermissao;

/// <summary>
/// Relatório detalhado de permissões por papel
/// </summary>
public class RelatorioPermissoesPapel
{
    /// <summary>
    /// Papel analisado
    /// </summary>
    public PapelResumoRelatorio Papel { get; set; } = new();

    /// <summary>
    /// Total de permissões do papel
    /// </summary>
    public int TotalPermissoes { get; set; }

    /// <summary>
    /// Permissões ativas
    /// </summary>
    public int PermissoesAtivas { get; set; }

    /// <summary>
    /// Permissões inativas
    /// </summary>
    public int PermissoesInativas { get; set; }

    /// <summary>
    /// Lista de permissões com detalhes
    /// </summary>
    public List<PermissaoDetalhada> Permissoes { get; set; } = new();

    /// <summary>
    /// Comparação com outros papéis
    /// </summary>
    public ComparacaoPapeis? Comparacao { get; set; }
}