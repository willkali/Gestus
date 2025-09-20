using Gestus.DTOs.Comuns;
using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Grupo;

/// <summary>
/// Request para busca avançada de grupos
/// </summary>
public class BuscaAvancadaGruposRequest : FiltrosBase
{
    /// <summary>
    /// Tipos de grupos para filtrar
    /// </summary>
    public List<string>? Tipos { get; set; }

    /// <summary>
    /// Status ativo/inativo
    /// </summary>
    public bool? Ativo { get; set; }

    /// <summary>
    /// Data de criação início
    /// </summary>
    public DateTime? DataCriacaoInicio { get; set; }

    /// <summary>
    /// Data de criação fim
    /// </summary>
    public DateTime? DataCriacaoFim { get; set; }

    /// <summary>
    /// Número mínimo de usuários
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Mínimo de usuários deve ser >= 0")]
    public int? MinUsuarios { get; set; }

    /// <summary>
    /// Número máximo de usuários
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Máximo de usuários deve ser >= 0")]
    public int? MaxUsuarios { get; set; }

    /// <summary>
    /// Buscar apenas grupos criados por usuário específico
    /// </summary>
    public int? CriadoPorUsuarioId { get; set; }

    /// <summary>
    /// Incluir grupos arquivados
    /// </summary>
    public bool IncluirArquivados { get; set; } = false;

    /// <summary>
    /// Filtros avançados de usuários
    /// </summary>
    public FiltroAvancadoUsuarios? FiltroUsuarios { get; set; }
}