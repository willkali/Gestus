namespace Gestus.DTOs.PapelPermissao;

/// <summary>
/// Tendências temporais
/// </summary>
public class TendenciasTempo
{
    public Dictionary<DateTime, int> AssociacoesPorDia { get; set; } = new();
}