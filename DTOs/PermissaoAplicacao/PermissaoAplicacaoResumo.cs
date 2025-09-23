namespace Gestus.DTOs.PermissaoAplicacao;

/// <summary>
/// Resumo de permissão de aplicação para listagens
/// </summary>
public class PermissaoAplicacaoResumo
{
    /// <summary>
    /// ID da permissão
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// ID da aplicação
    /// </summary>
    public int AplicacaoId { get; set; }

    /// <summary>
    /// Nome da aplicação
    /// </summary>
    public string NomeAplicacao { get; set; } = string.Empty;

    /// <summary>
    /// Código da aplicação
    /// </summary>
    public string CodigoAplicacao { get; set; } = string.Empty;

    /// <summary>
    /// Nome da permissão
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Descrição da permissão
    /// </summary>
    public string Descricao { get; set; } = string.Empty;

    /// <summary>
    /// Recurso controlado
    /// </summary>
    public string Recurso { get; set; } = string.Empty;

    /// <summary>
    /// Ação permitida
    /// </summary>
    public string Acao { get; set; } = string.Empty;

    /// <summary>
    /// Categoria da permissão
    /// </summary>
    public string? Categoria { get; set; }

    /// <summary>
    /// Nível de privilégio (1-10)
    /// </summary>
    public int Nivel { get; set; }

    /// <summary>
    /// Status ativo/inativo
    /// </summary>
    public bool Ativa { get; set; }

    /// <summary>
    /// Campo específico mais relevante baseado no tipo da aplicação
    /// </summary>
    public string? CampoEspecifico { get; set; }

    /// <summary>
    /// Valor do campo específico
    /// </summary>
    public string? ValorCampoEspecifico { get; set; }

    /// <summary>
    /// Total de papéis que possuem esta permissão
    /// </summary>
    public int TotalPapeis { get; set; }

    /// <summary>
    /// Data de criação
    /// </summary>
    public DateTime DataCriacao { get; set; }

    /// <summary>
    /// Tipo da aplicação
    /// </summary>
    public TipoAplicacaoPermissao TipoAplicacao { get; set; } = new();
}