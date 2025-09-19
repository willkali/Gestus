using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Usuario;

/// <summary>
/// Filtros avançados para listagem de usuários
/// </summary>
public class FiltrosUsuario
{
    /// <summary>
    /// Filtro por email (busca parcial)
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Filtro por nome/sobrenome (busca parcial)
    /// </summary>
    public string? Nome { get; set; }

    /// <summary>
    /// Filtro por status ativo/inativo
    /// </summary>
    public bool? Ativo { get; set; }

    /// <summary>
    /// Data início para filtrar por data de criação
    /// </summary>
    public DateTime? DataCriacaoInicio { get; set; }

    /// <summary>
    /// Data fim para filtrar por data de criação
    /// </summary>
    public DateTime? DataCriacaoFim { get; set; }

    /// <summary>
    /// Filtrar por papéis específicos
    /// </summary>
    public List<string>? Papeis { get; set; }

    /// <summary>
    /// Campo para ordenação
    /// </summary>
    public string? OrdenarPor { get; set; } = "DataCriacao";

    /// <summary>
    /// Direção da ordenação (asc/desc)
    /// </summary>
    public string? DirecaoOrdenacao { get; set; } = "desc";

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

    public override string ToString()
    {
        return $"Email={Email}, Nome={Nome}, Ativo={Ativo}, Papeis=[{string.Join(",", Papeis ?? new())}]";
    }
}