namespace Gestus.DTOs.PermissaoAplicacao;

/// <summary>
/// Resumo de papel que possui uma permissão de aplicação
/// </summary>
public class PapelPermissaoAplicacaoResumo
{
    /// <summary>
    /// ID do papel
    /// </summary>
    public int PapelId { get; set; }

    /// <summary>
    /// Nome do papel
    /// </summary>
    public string NomePapel { get; set; } = string.Empty;

    /// <summary>
    /// Descrição do papel
    /// </summary>
    public string DescricaoPapel { get; set; } = string.Empty;

    /// <summary>
    /// Categoria do papel
    /// </summary>
    public string? CategoriaPapel { get; set; }

    /// <summary>
    /// Nível do papel
    /// </summary>
    public int NivelPapel { get; set; }

    /// <summary>
    /// Status ativo/inativo do papel
    /// </summary>
    public bool PapelAtivo { get; set; }

    /// <summary>
    /// Data de atribuição da permissão ao papel
    /// </summary>
    public DateTime DataAtribuicao { get; set; }

    /// <summary>
    /// Data de expiração da atribuição
    /// </summary>
    public DateTime? DataExpiracao { get; set; }

    /// <summary>
    /// Status da atribuição
    /// </summary>
    public bool AtribuicaoAtiva { get; set; }

    /// <summary>
    /// Nome de quem atribuiu
    /// </summary>
    public string? AtribuidoPor { get; set; }

    /// <summary>
    /// Observações da atribuição
    /// </summary>
    public string? Observacoes { get; set; }

    /// <summary>
    /// Total de usuários com este papel
    /// </summary>
    public int TotalUsuarios { get; set; }
}