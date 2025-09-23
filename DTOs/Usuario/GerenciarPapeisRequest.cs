using System.ComponentModel.DataAnnotations;
using Gestus.DTOs.Permissao;

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
/// Estatísticas da operação de papéis
/// </summary>
/// <summary>
/// Estatísticas de uma operação de usuário
/// </summary>
public class EstatisticasOperacao
{
    /// <summary>
    /// Duração da operação
    /// </summary>
    public TimeSpan Duracao { get; set; }

    /// <summary>
    /// Recursos afetados (ex: usuários processados)
    /// </summary>
    public int RecursosAfetados { get; set; }

    /// <summary>
    /// Recursos processados com sucesso
    /// </summary>
    public int RecursosSucesso { get; set; }

    /// <summary>
    /// Recursos que falharam
    /// </summary>
    public int RecursosFalha { get; set; }

    /// <summary>
    /// Data e hora da operação
    /// </summary>
    public DateTime DataOperacao { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Dados adicionais da operação específicos para usuários
    /// </summary>
    public Dictionary<string, object>? DadosAdicionais { get; set; }

    /// <summary>
    /// Total de papéis afetados
    /// </summary>
    public int TotalPapeisAfetados { get; set; }

    /// <summary>
    /// Total de aplicações afetadas
    /// </summary>
    public int TotalAplicacoesAfetadas { get; set; }

    /// <summary>
    /// Total de permissões resultantes
    /// </summary>
    public int TotalPermissoes { get; set; }

    /// <summary>
    /// Total de papéis antes da operação
    /// </summary>
    public int TotalPapeisAntes { get; set; }

    /// <summary>
    /// Total de papéis depois da operação
    /// </summary>
    public int TotalPapeisDepois { get; set; }

    /// <summary>
    /// Número de papéis adicionados na operação
    /// </summary>
    public int PapeisAdicionados { get; set; }

    /// <summary>
    /// Número de papéis removidos na operação
    /// </summary>
    public int PapeisRemovidos { get; set; }
}

/// <summary>
/// Resultado de operação interna
/// </summary>
public class ResultadoOperacao
{
    public bool Sucesso { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public List<string> Detalhes { get; set; } = new();
    
    /// <summary>
    /// Estatísticas da operação
    /// </summary>
    public EstatisticasOperacao? Estatisticas { get; set; }

    /// <summary>
    /// Lista de alertas gerados
    /// </summary>
    public List<string> Alertas { get; set; } = new();

    /// <summary>
    /// Lista de erros detalhados
    /// </summary>
    public List<string> Erros { get; set; } = new();
}