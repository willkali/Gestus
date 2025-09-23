using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.PermissaoAplicacao;

/// <summary>
/// Request para criação de nova permissão de aplicação
/// </summary>
public class CriarPermissaoAplicacaoRequest
{
    /// <summary>
    /// ID da aplicação
    /// </summary>
    [Required(ErrorMessage = "ID da aplicação é obrigatório")]
    [Range(1, int.MaxValue, ErrorMessage = "ID da aplicação deve ser válido")]
    public int AplicacaoId { get; set; }

    /// <summary>
    /// Nome da permissão (formato: recurso.acao)
    /// </summary>
    [Required(ErrorMessage = "Nome da permissão é obrigatório")]
    [MaxLength(100, ErrorMessage = "Nome deve ter no máximo 100 caracteres")]
    [RegularExpression(@"^[a-zA-Z0-9_-]+\.[a-zA-Z0-9_-]+$", ErrorMessage = "Nome deve seguir o formato: recurso.acao")]
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Descrição da permissão
    /// </summary>
    [Required(ErrorMessage = "Descrição é obrigatória")]
    [MaxLength(200, ErrorMessage = "Descrição deve ter no máximo 200 caracteres")]
    public string Descricao { get; set; } = string.Empty;

    /// <summary>
    /// Recurso controlado
    /// </summary>
    [Required(ErrorMessage = "Recurso é obrigatório")]
    [MaxLength(100, ErrorMessage = "Recurso deve ter no máximo 100 caracteres")]
    public string Recurso { get; set; } = string.Empty;

    /// <summary>
    /// Ação permitida
    /// </summary>
    [Required(ErrorMessage = "Ação é obrigatória")]
    [MaxLength(50, ErrorMessage = "Ação deve ter no máximo 50 caracteres")]
    public string Acao { get; set; } = string.Empty;

    /// <summary>
    /// Categoria da permissão
    /// </summary>
    [MaxLength(100, ErrorMessage = "Categoria deve ter no máximo 100 caracteres")]
    public string? Categoria { get; set; }

    /// <summary>
    /// Nível de privilégio necessário (1-10)
    /// </summary>
    [Range(1, 10, ErrorMessage = "Nível deve estar entre 1 e 10")]
    public int Nivel { get; set; } = 1;

    // Campos específicos por tipo de aplicação
    /// <summary>
    /// Para aplicações HTTP - endpoint específico
    /// </summary>
    [MaxLength(200, ErrorMessage = "Endpoint deve ter no máximo 200 caracteres")]
    public string? Endpoint { get; set; }

    /// <summary>
    /// Para aplicações HTTP - método HTTP
    /// </summary>
    [MaxLength(20, ErrorMessage = "Método HTTP deve ter no máximo 20 caracteres")]
    public string? MetodoHttp { get; set; }

    /// <summary>
    /// Para aplicações Desktop - módulo interno
    /// </summary>
    [MaxLength(100, ErrorMessage = "Módulo deve ter no máximo 100 caracteres")]
    public string? Modulo { get; set; }

    /// <summary>
    /// Para aplicações Desktop/Mobile - tela específica
    /// </summary>
    [MaxLength(100, ErrorMessage = "Tela deve ter no máximo 100 caracteres")]
    public string? Tela { get; set; }

    /// <summary>
    /// Para aplicações CLI - comando específico
    /// </summary>
    [MaxLength(100, ErrorMessage = "Comando deve ter no máximo 100 caracteres")]
    public string? Comando { get; set; }

    /// <summary>
    /// Para aplicações de Banco - operação SQL
    /// </summary>
    [MaxLength(50, ErrorMessage = "Operação SQL deve ter no máximo 50 caracteres")]
    public string? OperacaoSql { get; set; }

    /// <summary>
    /// Para aplicações de Banco - schema específico
    /// </summary>
    [MaxLength(100, ErrorMessage = "Schema deve ter no máximo 100 caracteres")]
    public string? Schema { get; set; }

    /// <summary>
    /// Para aplicações de Banco - tabela específica
    /// </summary>
    [MaxLength(100, ErrorMessage = "Tabela deve ter no máximo 100 caracteres")]
    public string? Tabela { get; set; }

    /// <summary>
    /// Condições adicionais (JSON)
    /// </summary>
    public string? Condicoes { get; set; }

    /// <summary>
    /// Criar permissões em lote baseadas em template
    /// </summary>
    public bool CriarEmLote { get; set; } = false;

    /// <summary>
    /// Template de permissões para criação em lote
    /// </summary>
    public List<TemplatePermissaoAplicacao>? TemplatesLote { get; set; }
}

/// <summary>
/// Template para criação de permissão em lote
/// </summary>
public class TemplatePermissaoAplicacao
{
    public string Recurso { get; set; } = string.Empty;
    public string Acao { get; set; } = string.Empty;
    public string? Categoria { get; set; }
    public int Nivel { get; set; } = 1;
    public Dictionary<string, string?> CamposEspecificos { get; set; } = new();
}