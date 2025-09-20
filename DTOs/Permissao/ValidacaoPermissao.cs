namespace Gestus.DTOs.Permissao;

/// <summary>
/// Resultado de validação de permissão
/// </summary>
public class ValidacaoPermissao
{
    /// <summary>
    /// Indica se a validação passou
    /// </summary>
    public bool Valida { get; set; }

    /// <summary>
    /// Lista de erros encontrados
    /// </summary>
    public List<string> Erros { get; set; } = new();

    /// <summary>
    /// Lista de avisos (não impedem a operação)
    /// </summary>
    public List<string> Avisos { get; set; } = new();

    /// <summary>
    /// Sugestões de correção
    /// </summary>
    public List<string> Sugestoes { get; set; } = new();
}