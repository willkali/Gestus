namespace Gestus.DTOs.PapelPermissao;

/// <summary>
/// Relatório de permissões órfãs
/// </summary>
public class RelatorioPermissoesOrfas
{
    public List<AssociacaoOrfa> AssociacoesComPapeisInativos { get; set; } = new();
    public List<AssociacaoOrfa> AssociacoesComPermissoesInativas { get; set; } = new();
    public List<PermissaoNaoUtilizada> PermissoesNuncaAtribuidas { get; set; } = new();
    public List<PapelSemPermissoes> PapeisSemPermissoes { get; set; } = new();
    public ResumoLimpeza Resumo { get; set; } = new();
}