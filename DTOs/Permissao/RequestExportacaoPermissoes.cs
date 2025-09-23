namespace Gestus.DTOs.Permissao;

/// <summary>
/// Request para exportação de permissões
/// </summary>
public class RequestExportacaoPermissoes
{
    /// <summary>
    /// Formato de exportação: csv, xlsx, json, xml
    /// </summary>
    public string Formato { get; set; } = "csv";

    /// <summary>
    /// IDs específicas para exportar (se vazio, exporta todos)
    /// </summary>
    public List<int>? PermissoesIds { get; set; }

    /// <summary>
    /// Filtros para exportação
    /// </summary>
    public FiltrosPermissao? Filtros { get; set; }

    /// <summary>
    /// Incluir permissões inativas
    /// </summary>
    public bool IncluirInativas { get; set; } = false;

    /// <summary>
    /// Incluir relacionamentos (papéis, usuários)
    /// </summary>
    public bool IncluirRelacionamentos { get; set; } = false;

    /// <summary>
    /// Incluir estatísticas de uso
    /// </summary>
    public bool IncluirEstatisticas { get; set; } = false;
}

/// <summary>
/// Request para importação de permissões
/// </summary>
public class RequestImportacaoPermissoes
{
    /// <summary>
    /// Formato dos dados: csv, xlsx, json, xml
    /// </summary>
    public string Formato { get; set; } = "csv";

    /// <summary>
    /// Dados para importação (base64 ou JSON)
    /// </summary>
    public string Dados { get; set; } = string.Empty;

    /// <summary>
    /// Modo de importação: criar, atualizar, substituir
    /// </summary>
    public string Modo { get; set; } = "criar";

    /// <summary>
    /// Ignorar erros de validação (não recomendado)
    /// </summary>
    public bool IgnorarErros { get; set; } = false;

    /// <summary>
    /// Validar apenas (não executar importação)
    /// </summary>
    public bool ApenasValidar { get; set; } = false;
}