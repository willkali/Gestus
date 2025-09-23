using System.ComponentModel.DataAnnotations;
using Gestus.DTOs.Comuns;  // ✅ Adicionado using

namespace Gestus.DTOs.PermissaoAplicacao;

/// <summary>
/// Filtros para busca de permissões de aplicação
/// </summary>
public class FiltrosPermissaoAplicacao : FiltrosBase  // ✅ Herda de FiltrosBase
{
    /// <summary>
    /// Filtro por ID da aplicação específica
    /// </summary>
    public int? AplicacaoId { get; set; }

    /// <summary>
    /// Filtro por código da aplicação
    /// </summary>
    [MaxLength(50)]
    public string? CodigoAplicacao { get; set; }

    /// <summary>
    /// Filtro por tipo de aplicação
    /// </summary>
    [MaxLength(50)]
    public string? TipoAplicacao { get; set; }

    /// <summary>
    /// Filtro por nome da permissão
    /// </summary>
    [MaxLength(100)]
    public string? Nome { get; set; }

    /// <summary>
    /// Filtro por descrição da permissão
    /// </summary>
    [MaxLength(200)]
    public string? Descricao { get; set; }

    /// <summary>
    /// Filtro por recurso
    /// </summary>
    [MaxLength(100)]
    public string? Recurso { get; set; }

    /// <summary>
    /// Filtro por ação
    /// </summary>
    [MaxLength(50)]
    public string? Acao { get; set; }

    /// <summary>
    /// Filtro por categoria
    /// </summary>
    [MaxLength(100)]
    public string? Categoria { get; set; }

    /// <summary>
    /// Filtro por nível mínimo
    /// </summary>
    [Range(1, 10)]
    public int? NivelMinimo { get; set; }

    /// <summary>
    /// Filtro por nível máximo
    /// </summary>
    [Range(1, 10)]
    public int? NivelMaximo { get; set; }

    /// <summary>
    /// Filtro por status ativo/inativo
    /// </summary>
    public bool? Ativa { get; set; }

    /// <summary>
    /// Filtro por endpoint (para aplicações HTTP)
    /// </summary>
    [MaxLength(200)]
    public string? Endpoint { get; set; }

    /// <summary>
    /// Filtro por método HTTP (para aplicações HTTP)
    /// </summary>
    [MaxLength(20)]
    public string? MetodoHttp { get; set; }

    /// <summary>
    /// Filtro por módulo (para aplicações Desktop)
    /// </summary>
    [MaxLength(100)]
    public string? Modulo { get; set; }

    /// <summary>
    /// Filtro por tela (para aplicações Desktop/Mobile)
    /// </summary>
    [MaxLength(100)]
    public string? Tela { get; set; }

    /// <summary>
    /// Filtro por comando (para aplicações CLI)
    /// </summary>
    [MaxLength(100)]
    public string? Comando { get; set; }

    /// <summary>
    /// Filtro por operação SQL (para aplicações de Banco)
    /// </summary>
    [MaxLength(50)]
    public string? OperacaoSql { get; set; }

    /// <summary>
    /// Filtro por schema (para aplicações de Banco)
    /// </summary>
    [MaxLength(100)]
    public string? Schema { get; set; }

    /// <summary>
    /// Filtro por tabela (para aplicações de Banco)
    /// </summary>
    [MaxLength(100)]
    public string? Tabela { get; set; }

    /// <summary>
    /// Data de criação início
    /// </summary>
    public DateTime? DataCriacaoInicio { get; set; }

    /// <summary>
    /// Data de criação fim
    /// </summary>
    public DateTime? DataCriacaoFim { get; set; }

    /// <summary>
    /// Incluir permissões inativas (alias para IncluirInativos de FiltrosBase)
    /// </summary>
    public bool IncluirInativas 
    { 
        get => IncluirInativos; 
        set => IncluirInativos = value; 
    }

    /// <summary>
    /// Filtrar permissões que estão em uso (atribuídas a papéis)
    /// </summary>
    public bool? EmUso { get; set; }

    /// <summary>
    /// Incluir estatísticas na resposta
    /// </summary>
    public bool IncluirEstatisticas { get; set; } = false;
}