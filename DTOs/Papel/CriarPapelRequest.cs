using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Papel;

/// <summary>
/// Dados para criação de um novo papel
/// </summary>
public class CriarPapelRequest
{
    /// <summary>
    /// Nome único do papel
    /// </summary>
    [Required(ErrorMessage = "Nome do papel é obrigatório")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Nome deve ter entre 2 e 100 caracteres")]
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Descrição detalhada do papel
    /// </summary>
    [Required(ErrorMessage = "Descrição do papel é obrigatória")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "Descrição deve ter entre 5 e 200 caracteres")]
    public string Descricao { get; set; } = string.Empty;

    /// <summary>
    /// Categoria do papel (Administração, Operação, etc.)
    /// </summary>
    [StringLength(100, ErrorMessage = "Categoria deve ter no máximo 100 caracteres")]
    public string? Categoria { get; set; }

    /// <summary>
    /// Nível hierárquico do papel (1 = mais alto)
    /// </summary>
    [Range(1, 999, ErrorMessage = "Nível deve estar entre 1 e 999")]
    public int Nivel { get; set; } = 100;

    /// <summary>
    /// Se o papel deve estar ativo após criação
    /// </summary>
    public bool Ativo { get; set; } = true;

    /// <summary>
    /// Lista de permissões a serem associadas ao papel
    /// </summary>
    public List<string>? Permissoes { get; set; }

    /// <summary>
    /// Observações sobre o papel
    /// </summary>
    [StringLength(500, ErrorMessage = "Observações devem ter no máximo 500 caracteres")]
    public string? Observacoes { get; set; }
}