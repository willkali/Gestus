namespace Gestus.DTOs.Aplicacao;

/// <summary>
/// Informações completas de status de aplicação
/// </summary>
public class StatusAplicacaoCompleto
{
    /// <summary>
    /// ID do status
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Código do status
    /// </summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>
    /// Nome do status
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Descrição do status
    /// </summary>
    public string Descricao { get; set; } = string.Empty;

    /// <summary>
    /// Cor de fundo
    /// </summary>
    public string CorFundo { get; set; } = string.Empty;

    /// <summary>
    /// Cor do texto
    /// </summary>
    public string CorTexto { get; set; } = string.Empty;

    /// <summary>
    /// Ícone do status
    /// </summary>
    public string? Icone { get; set; }

    /// <summary>
    /// Permite acesso dos usuários
    /// </summary>
    public bool PermiteAcesso { get; set; }

    /// <summary>
    /// Permite novos usuários
    /// </summary>
    public bool PermiteNovoUsuario { get; set; }

    /// <summary>
    /// Visível para usuários normais
    /// </summary>
    public bool VisivelParaUsuarios { get; set; }

    /// <summary>
    /// Mensagem exibida aos usuários
    /// </summary>
    public string? MensagemUsuario { get; set; }

    /// <summary>
    /// Prioridade do status (1-10)
    /// </summary>
    public int Prioridade { get; set; }
}