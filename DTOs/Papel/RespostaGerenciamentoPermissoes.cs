namespace Gestus.DTOs.Papel;

/// <summary>
/// Resposta do gerenciamento de permissões
/// </summary>
public class RespostaGerenciamentoPermissoes
{
    /// <summary>
    /// Indica se a operação foi bem-sucedida
    /// </summary>
    public bool Sucesso { get; set; }

    /// <summary>
    /// Mensagem sobre o resultado
    /// </summary>
    public string Mensagem { get; set; } = string.Empty;

    /// <summary>
    /// Operação realizada
    /// </summary>
    public string Operacao { get; set; } = string.Empty;

    /// <summary>
    /// Total de permissões afetadas
    /// </summary>
    public int PermissoesAfetadas { get; set; }

    /// <summary>
    /// Lista atual de permissões do papel após a operação
    /// </summary>
    public List<PermissaoPapel> PermissoesAtuais { get; set; } = new();

    /// <summary>
    /// Permissões que foram adicionadas
    /// </summary>
    public List<string> PermissoesAdicionadas { get; set; } = new();

    /// <summary>
    /// Permissões que foram removidas
    /// </summary>
    public List<string> PermissoesRemovidas { get; set; } = new();

    /// <summary>
    /// Observações da operação
    /// </summary>
    public string? Observacoes { get; set; }

    /// <summary>
    /// Data e hora da operação
    /// </summary>
    public DateTime DataOperacao { get; set; } = DateTime.UtcNow;
}