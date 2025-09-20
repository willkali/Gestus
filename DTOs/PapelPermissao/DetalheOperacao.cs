namespace Gestus.DTOs.PapelPermissao;

/// <summary>
/// Detalhe de uma operação individual
/// </summary>
public class DetalheOperacao
{
    /// <summary>
    /// ID do papel
    /// </summary>
    public int PapelId { get; set; }

    /// <summary>
    /// ID da permissão
    /// </summary>
    public int PermissaoId { get; set; }

    /// <summary>
    /// Indica se foi bem-sucedida
    /// </summary>
    public bool Sucesso { get; set; }

    /// <summary>
    /// Mensagem de erro (se houver)
    /// </summary>
    public string? Erro { get; set; }
}