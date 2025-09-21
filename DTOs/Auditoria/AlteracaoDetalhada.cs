namespace Gestus.DTOs.Auditoria;

/// <summary>
/// Detalhes de uma alteração específica
/// </summary>
public class AlteracaoDetalhada
{
    public string Campo { get; set; } = string.Empty;
    public object? ValorAnterior { get; set; }
    public object? ValorNovo { get; set; }
    public string TipoAlteracao { get; set; } = string.Empty; // Criação, Edição, Remoção
}