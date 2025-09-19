using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Usuario;

/// <summary>
/// Request para gerenciamento de papéis de usuário
/// </summary>
public class GerenciarPapeisRequest
{
    /// <summary>
    /// Tipo de operação: "substituir", "adicionar", "remover", "limpar"
    /// </summary>
    [Required(ErrorMessage = "Operação é obrigatória")]
    public string Operacao { get; set; } = string.Empty;

    /// <summary>
    /// Lista de papéis para a operação (não obrigatório para "limpar")
    /// </summary>
    public List<string> Papeis { get; set; } = new();

    /// <summary>
    /// Observações sobre a operação
    /// </summary>
    [MaxLength(500, ErrorMessage = "Observações devem ter no máximo 500 caracteres")]
    public string? Observacoes { get; set; }
}

/// <summary>
/// Resposta do gerenciamento de papéis
/// </summary>
public class RespostaGerenciamentoPapeis
{
    public bool Sucesso { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public UsuarioResumo Usuario { get; set; } = new();
    public List<PapelComDetalhes> PapeisAtuais { get; set; } = new();
    public EstatisticasOperacao Estatisticas { get; set; } = new();
}

/// <summary>
/// Papel com detalhes completos
/// </summary>
public class PapelComDetalhes
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string? Categoria { get; set; }
    public int Nivel { get; set; }
    public DateTime DataAtribuicao { get; set; }
    public DateTime? DataExpiracao { get; set; }
    public int? AtribuidoPorId { get; set; }
    public List<PermissaoResumo> Permissoes { get; set; } = new();
}

/// <summary>
/// Papel disponível no sistema
/// </summary>
public class PapelDisponivel
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string? Categoria { get; set; }
    public int Nivel { get; set; }
    public int TotalPermissoes { get; set; }
    public List<PermissaoResumo> Permissoes { get; set; } = new();
}

/// <summary>
/// Permissão resumida
/// </summary>
public class PermissaoResumo
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string? Categoria { get; set; }
}

/// <summary>
/// Estatísticas da operação de papéis
/// </summary>
public class EstatisticasOperacao
{
    public int TotalPapeisAntes { get; set; }
    public int TotalPapeisDepois { get; set; }
    public List<string> PapeisAdicionados { get; set; } = new();
    public List<string> PapeisRemovidos { get; set; } = new();
    public int TotalPermissoes { get; set; }
}

/// <summary>
/// Resultado de operação interna
/// </summary>
public class ResultadoOperacao
{
    public bool Sucesso { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public List<string> Detalhes { get; set; } = new();
}