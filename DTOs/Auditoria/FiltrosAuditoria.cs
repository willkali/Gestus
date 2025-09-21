using Gestus.DTOs.Comuns;
using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Auditoria;

/// <summary>
/// Filtros para consulta de registros de auditoria
/// </summary>
public class FiltrosAuditoria : FiltrosBase
{
    /// <summary>
    /// ID do usuário que executou a ação
    /// </summary>
    public int? UsuarioId { get; set; }

    /// <summary>
    /// Nome do usuário para busca
    /// </summary>
    public string? NomeUsuario { get; set; }

    /// <summary>
    /// Email do usuário para busca
    /// </summary>
    public string? EmailUsuario { get; set; }

    /// <summary>
    /// Ação executada (ex: Criar, Editar, Excluir)
    /// </summary>
    public string? Acao { get; set; }

    /// <summary>
    /// Recurso afetado (ex: Usuario, Papel, Grupo)
    /// </summary>
    public string? Recurso { get; set; }

    /// <summary>
    /// ID específico do recurso
    /// </summary>
    public string? RecursoId { get; set; }

    /// <summary>
    /// Data/hora início do período
    /// </summary>
    public DateTime? DataInicio { get; set; }

    /// <summary>
    /// Data/hora fim do período
    /// </summary>
    public DateTime? DataFim { get; set; }

    /// <summary>
    /// Endereço IP para filtrar
    /// </summary>
    public string? EnderecoIp { get; set; }

    /// <summary>
    /// Busca em observações
    /// </summary>
    public string? BuscaObservacoes { get; set; }

    /// <summary>
    /// Incluir apenas registros com alterações de dados
    /// </summary>
    public bool? ApenasComAlteracoes { get; set; }

    /// <summary>
    /// Filtrar por categorias de ação
    /// </summary>
    public List<string>? CategoriasAcao { get; set; }
}