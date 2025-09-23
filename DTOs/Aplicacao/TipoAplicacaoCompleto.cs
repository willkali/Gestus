namespace Gestus.DTOs.Aplicacao;

/// <summary>
/// Informações completas de tipo de aplicação
/// </summary>
public class TipoAplicacaoCompleto
{
    /// <summary>
    /// ID do tipo
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Código do tipo
    /// </summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>
    /// Nome do tipo
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Descrição do tipo
    /// </summary>
    public string Descricao { get; set; } = string.Empty;

    /// <summary>
    /// Ícone do tipo
    /// </summary>
    public string? Icone { get; set; }

    /// <summary>
    /// Cor do tipo
    /// </summary>
    public string? Cor { get; set; }

    /// <summary>
    /// Nível de complexidade (1-5)
    /// </summary>
    public int NivelComplexidade { get; set; }

    /// <summary>
    /// Instruções de integração
    /// </summary>
    public string? InstrucoesIntegracao { get; set; }

    /// <summary>
    /// Campos de permissão suportados (JSON)
    /// </summary>
    public string CamposPermissao { get; set; } = string.Empty;

    /// <summary>
    /// Configurações específicas do tipo (JSON)
    /// </summary>
    public string? ConfiguracoesTipo { get; set; }
}