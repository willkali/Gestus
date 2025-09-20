namespace Gestus.DTOs.PapelPermissao;

/// <summary>
/// Resultado de limpeza
/// </summary>
public class ResultadoLimpeza
{
    public int ItensLimpos { get; set; }
    public List<string> Erros { get; set; } = new();
}