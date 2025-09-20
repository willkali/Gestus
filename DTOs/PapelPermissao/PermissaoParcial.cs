namespace Gestus.DTOs.PapelPermissao;

/// <summary>
/// Permissão parcial (em alguns papéis, mas não todos)
/// </summary>
public class PermissaoParcial
{
    public PermissaoDetalhada Permissao { get; set; } = new();
    public List<string> PapeisQuetem { get; set; } = new();
}