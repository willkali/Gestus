namespace Gestus.DTOs.Papel;

/// <summary>
/// Permissão associada a um papel
/// </summary>
public class PermissaoPapel
{
    /// <summary>
    /// ID da permissão
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Nome da permissão
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Descrição da permissão
    /// </summary>
    public string Descricao { get; set; } = string.Empty;

    /// <summary>
    /// Recurso da permissão (ex: Usuarios, Papeis)
    /// </summary>
    public string Recurso { get; set; } = string.Empty;

    /// <summary>
    /// Ação da permissão (ex: Criar, Editar, Listar)
    /// </summary>
    public string Acao { get; set; } = string.Empty;

    /// <summary>
    /// Categoria da permissão
    /// </summary>
    public string? Categoria { get; set; }

    /// <summary>
    /// Data em que a permissão foi atribuída ao papel
    /// </summary>
    public DateTime DataAtribuicao { get; set; }

    /// <summary>
    /// Nome formatado da permissão (Recurso.Ação)
    /// </summary>
    public string NomeFormatado => $"{Recurso}.{Acao}";

    /// <summary>
    /// Indica se é uma permissão crítica do sistema
    /// </summary>
    public bool PermissaoCritica => Nome.StartsWith("Sistema.") || 
                                   Nome.Contains("Excluir") || 
                                   Nome.Contains("GerenciarPapeis");
}