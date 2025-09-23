using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.PermissaoAplicacao;

/// <summary>
/// Request para exportação de permissões de aplicação
/// </summary>
public class ExportarPermissoesAplicacaoRequest
{
    /// <summary>
    /// Formato de exportação: csv, xlsx, json, xml
    /// </summary>
    [Required(ErrorMessage = "Formato é obrigatório")]
    public string Formato { get; set; } = "csv";

    /// <summary>
    /// ID da aplicação específica (opcional)
    /// </summary>
    public int? AplicacaoId { get; set; }

    /// <summary>
    /// IDs específicas para exportar (se vazio, exporta conforme filtros)
    /// </summary>
    public List<int>? PermissoesIds { get; set; }

    /// <summary>
    /// Filtros para exportação
    /// </summary>
    public FiltrosPermissaoAplicacao? Filtros { get; set; }

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

    /// <summary>
    /// Incluir campos específicos por tipo de aplicação
    /// </summary>
    public bool IncluirCamposEspecificos { get; set; } = true;

    /// <summary>
    /// Incluir condições e configurações avançadas
    /// </summary>
    public bool IncluirCondicoes { get; set; } = false;

    /// <summary>
    /// Nome do arquivo (sem extensão)
    /// </summary>
    [MaxLength(100, ErrorMessage = "Nome do arquivo deve ter no máximo 100 caracteres")]
    public string? NomeArquivo { get; set; }
}

/// <summary>
/// Request para importação de permissões de aplicação
/// </summary>
public class ImportarPermissoesAplicacaoRequest
{
    /// <summary>
    /// ID da aplicação de destino
    /// </summary>
    [Required(ErrorMessage = "ID da aplicação é obrigatório")]
    [Range(1, int.MaxValue, ErrorMessage = "ID da aplicação deve ser válido")]
    public int AplicacaoId { get; set; }

    /// <summary>
    /// Formato dos dados: csv, xlsx, json, xml
    /// </summary>
    [Required(ErrorMessage = "Formato é obrigatório")]
    public string Formato { get; set; } = "csv";

    /// <summary>
    /// Dados para importação (base64 ou JSON)
    /// </summary>
    [Required(ErrorMessage = "Dados são obrigatórios")]
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

    /// <summary>
    /// Notificar administradores sobre a importação
    /// </summary>
    public bool NotificarAdministradores { get; set; } = true;

    /// <summary>
    /// Criar backup antes da importação
    /// </summary>
    public bool CriarBackup { get; set; } = true;
}