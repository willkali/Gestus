using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Usuario;

/// <summary>
/// Solicitação para busca avançada de usuários
/// </summary>
public class SolicitacaoBuscaAvancada
{
    /// <summary>
    /// Texto geral para busca em múltiplos campos
    /// </summary>
    [MaxLength(200, ErrorMessage = "Texto geral deve ter no máximo 200 caracteres")]
    public string? TextoGeral { get; set; }

    /// <summary>
    /// Filtro específico por email
    /// </summary>
    [MaxLength(256, ErrorMessage = "Email deve ter no máximo 256 caracteres")]
    public string? Email { get; set; }

    /// <summary>
    /// Filtro específico por nome/sobrenome
    /// </summary>
    [MaxLength(200, ErrorMessage = "Nome deve ter no máximo 200 caracteres")]
    public string? Nome { get; set; }

    /// <summary>
    /// Filtro específico por telefone
    /// </summary>
    [MaxLength(20, ErrorMessage = "Telefone deve ter no máximo 20 caracteres")]
    public string? Telefone { get; set; }

    /// <summary>
    /// Filtrar por status ativo/inativo
    /// </summary>
    public bool? Ativo { get; set; }

    /// <summary>
    /// Filtrar por email confirmado
    /// </summary>
    public bool? EmailConfirmado { get; set; }

    /// <summary>
    /// Filtrar por telefone confirmado
    /// </summary>
    public bool? TelefoneConfirmado { get; set; }

    /// <summary>
    /// Data início para criação
    /// </summary>
    public DateTime? DataCriacaoInicio { get; set; }

    /// <summary>
    /// Data fim para criação
    /// </summary>
    public DateTime? DataCriacaoFim { get; set; }

    /// <summary>
    /// Data início para último login
    /// </summary>
    public DateTime? UltimoLoginInicio { get; set; }

    /// <summary>
    /// Data fim para último login
    /// </summary>
    public DateTime? UltimoLoginFim { get; set; }

    /// <summary>
    /// Lista de papéis para filtrar
    /// </summary>
    public List<string>? Papeis { get; set; }

    /// <summary>
    /// Operador para papéis: "E" (todos) ou "OU" (qualquer)
    /// </summary>
    public string OperadorPapeis { get; set; } = "E";

    /// <summary>
    /// Lista de grupos para filtrar
    /// </summary>
    public List<string>? Grupos { get; set; }

    /// <summary>
    /// Operador para grupos: "E" (todos) ou "OU" (qualquer)
    /// </summary>
    public string OperadorGrupos { get; set; } = "E";

    /// <summary>
    /// Lista de permissões para filtrar
    /// </summary>
    public List<string>? Permissoes { get; set; }

    /// <summary>
    /// Operador para permissões: "E" (todas) ou "OU" (qualquer)
    /// </summary>
    public string OperadorPermissoes { get; set; } = "E";

    /// <summary>
    /// Buscar apenas usuários sem papéis
    /// </summary>
    public bool? SemPapeis { get; set; }

    /// <summary>
    /// Buscar apenas usuários sem grupos
    /// </summary>
    public bool? SemGrupos { get; set; }

    /// <summary>
    /// Buscar apenas usuários que nunca fizeram login
    /// </summary>
    public bool? SemUltimoLogin { get; set; }

    /// <summary>
    /// Critérios de ordenação
    /// </summary>
    public List<CriterioOrdenacao>? Ordenacao { get; set; }

    /// <summary>
    /// Página atual (baseada em 1)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Página deve ser maior que 0")]
    public int Pagina { get; set; } = 1;

    /// <summary>
    /// Itens por página
    /// </summary>
    [Range(1, 100, ErrorMessage = "Itens por página deve ser entre 1 e 100")]
    public int ItensPorPagina { get; set; } = 20;
}