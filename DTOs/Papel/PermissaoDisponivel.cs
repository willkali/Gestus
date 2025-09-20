namespace Gestus.DTOs.Papel;

/// <summary>
/// Permissão disponível para atribuição
/// </summary>
public class PermissaoDisponivel
{
    /// <summary>
    /// ID da permissão
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Nome único da permissão
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Descrição da permissão
    /// </summary>
    public string Descricao { get; set; } = string.Empty;

    /// <summary>
    /// Recurso da permissão
    /// </summary>
    public string Recurso { get; set; } = string.Empty;

    /// <summary>
    /// Ação da permissão
    /// </summary>
    public string Acao { get; set; } = string.Empty;

    /// <summary>
    /// Categoria da permissão
    /// </summary>
    public string? Categoria { get; set; }

    /// <summary>
    /// Indica se a permissão já está atribuída ao papel atual
    /// </summary>
    public bool JaAtribuida { get; set; }

    /// <summary>
    /// Indica se é uma permissão obrigatória para o papel
    /// </summary>
    public bool Obrigatoria { get; set; }

    /// <summary>
    /// Indica se é uma permissão crítica
    /// </summary>
    public bool Critica { get; set; }

    /// <summary>
    /// Total de papéis que possuem esta permissão
    /// </summary>
    public int TotalPapeisComPermissao { get; set; }
}