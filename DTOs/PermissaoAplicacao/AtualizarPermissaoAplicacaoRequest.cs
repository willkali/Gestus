using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.PermissaoAplicacao;

/// <summary>
/// Request para atualização de permissão de aplicação
/// </summary>
public class AtualizarPermissaoAplicacaoRequest
{
    /// <summary>
    /// Nova descrição da permissão
    /// </summary>
    [MaxLength(200, ErrorMessage = "Descrição deve ter no máximo 200 caracteres")]
    public string? Descricao { get; set; }

    /// <summary>
    /// Nova categoria da permissão
    /// </summary>
    [MaxLength(100, ErrorMessage = "Categoria deve ter no máximo 100 caracteres")]
    public string? Categoria { get; set; }

    /// <summary>
    /// Novo nível de privilégio (1-10)
    /// </summary>
    [Range(1, 10, ErrorMessage = "Nível deve estar entre 1 e 10")]
    public int? Nivel { get; set; }

    /// <summary>
    /// Novo status ativo/inativo
    /// </summary>
    public bool? Ativa { get; set; }

    // Campos específicos por tipo de aplicação
    /// <summary>
    /// Novo endpoint (para aplicações HTTP)
    /// </summary>
    [MaxLength(200, ErrorMessage = "Endpoint deve ter no máximo 200 caracteres")]
    public string? Endpoint { get; set; }

    /// <summary>
    /// Novo método HTTP (para aplicações HTTP)
    /// </summary>
    [MaxLength(20, ErrorMessage = "Método HTTP deve ter no máximo 20 caracteres")]
    public string? MetodoHttp { get; set; }

    /// <summary>
    /// Novo módulo (para aplicações Desktop)
    /// </summary>
    [MaxLength(100, ErrorMessage = "Módulo deve ter no máximo 100 caracteres")]
    public string? Modulo { get; set; }

    /// <summary>
    /// Nova tela (para aplicações Desktop/Mobile)
    /// </summary>
    [MaxLength(100, ErrorMessage = "Tela deve ter no máximo 100 caracteres")]
    public string? Tela { get; set; }

    /// <summary>
    /// Novo comando (para aplicações CLI)
    /// </summary>
    [MaxLength(100, ErrorMessage = "Comando deve ter no máximo 100 caracteres")]
    public string? Comando { get; set; }

    /// <summary>
    /// Nova operação SQL (para aplicações de Banco)
    /// </summary>
    [MaxLength(50, ErrorMessage = "Operação SQL deve ter no máximo 50 caracteres")]
    public string? OperacaoSql { get; set; }

    /// <summary>
    /// Novo schema (para aplicações de Banco)
    /// </summary>
    [MaxLength(100, ErrorMessage = "Schema deve ter no máximo 100 caracteres")]
    public string? Schema { get; set; }

    /// <summary>
    /// Nova tabela (para aplicações de Banco)
    /// </summary>
    [MaxLength(100, ErrorMessage = "Tabela deve ter no máximo 100 caracteres")]
    public string? Tabela { get; set; }

    /// <summary>
    /// Novas condições adicionais (JSON)
    /// </summary>
    public string? Condicoes { get; set; }

    /// <summary>
    /// Motivo da alteração
    /// </summary>
    [MaxLength(500, ErrorMessage = "Motivo deve ter no máximo 500 caracteres")]
    public string? MotivoAlteracao { get; set; }
}