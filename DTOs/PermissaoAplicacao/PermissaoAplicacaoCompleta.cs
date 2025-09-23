namespace Gestus.DTOs.PermissaoAplicacao;

/// <summary>
/// Informações completas de uma permissão de aplicação
/// </summary>
public class PermissaoAplicacaoCompleta
{
    /// <summary>
    /// ID da permissão
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// ID da aplicação
    /// </summary>
    public int AplicacaoId { get; set; }

    /// <summary>
    /// Nome da aplicação
    /// </summary>
    public string NomeAplicacao { get; set; } = string.Empty;

    /// <summary>
    /// Código da aplicação
    /// </summary>
    public string CodigoAplicacao { get; set; } = string.Empty;

    /// <summary>
    /// Nome da permissão (formato: recurso.acao)
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Descrição da permissão
    /// </summary>
    public string Descricao { get; set; } = string.Empty;

    /// <summary>
    /// Recurso controlado pela permissão
    /// </summary>
    public string Recurso { get; set; } = string.Empty;

    /// <summary>
    /// Ação permitida pela permissão
    /// </summary>
    public string Acao { get; set; } = string.Empty;

    /// <summary>
    /// Categoria da permissão
    /// </summary>
    public string? Categoria { get; set; }

    /// <summary>
    /// Nível de privilégio necessário (1-10)
    /// </summary>
    public int Nivel { get; set; }

    /// <summary>
    /// Status ativo/inativo
    /// </summary>
    public bool Ativa { get; set; }

    // Campos específicos por tipo de aplicação
    /// <summary>
    /// Para aplicações HTTP - endpoint específico
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Para aplicações HTTP - método HTTP
    /// </summary>
    public string? MetodoHttp { get; set; }

    /// <summary>
    /// Para aplicações Desktop/Compiladas - módulo interno
    /// </summary>
    public string? Modulo { get; set; }

    /// <summary>
    /// Para aplicações Desktop/Mobile - tela/formulário específico
    /// </summary>
    public string? Tela { get; set; }

    /// <summary>
    /// Para aplicações CLI - comando específico
    /// </summary>
    public string? Comando { get; set; }

    /// <summary>
    /// Para aplicações de Banco - operação SQL
    /// </summary>
    public string? OperacaoSql { get; set; }

    /// <summary>
    /// Para aplicações de Banco - schema/database específico
    /// </summary>
    public string? Schema { get; set; }

    /// <summary>
    /// Para aplicações de Banco - tabela/collection específica
    /// </summary>
    public string? Tabela { get; set; }

    /// <summary>
    /// Condições adicionais para a permissão (JSON)
    /// </summary>
    public string? Condicoes { get; set; }

    /// <summary>
    /// Data de criação
    /// </summary>
    public DateTime DataCriacao { get; set; }

    /// <summary>
    /// Data de última atualização
    /// </summary>
    public DateTime? DataAtualizacao { get; set; }

    /// <summary>
    /// Nome de quem atualizou
    /// </summary>
    public string? AtualizadoPor { get; set; }

    /// <summary>
    /// Total de papéis que possuem esta permissão
    /// </summary>
    public int TotalPapeis { get; set; }

    /// <summary>
    /// Total de usuários que possuem esta permissão (através dos papéis)
    /// </summary>
    public int TotalUsuarios { get; set; }

    /// <summary>
    /// Lista de papéis que possuem esta permissão
    /// </summary>
    public List<PapelPermissaoAplicacaoResumo> Papeis { get; set; } = new();

    /// <summary>
    /// Informações do tipo de aplicação
    /// </summary>
    public TipoAplicacaoPermissao TipoAplicacao { get; set; } = new();

    /// <summary>
    /// Estatísticas de uso da permissão
    /// </summary>
    public EstatisticasPermissaoAplicacao Estatisticas { get; set; } = new();
}

/// <summary>
/// Tipo de aplicação para contexto de permissão
/// </summary>
public class TipoAplicacaoPermissao
{
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string? Icone { get; set; }
    public string? Cor { get; set; }
    public List<string> CamposSuportados { get; set; } = new();
}

/// <summary>
/// Estatísticas de uso de uma permissão de aplicação
/// </summary>
public class EstatisticasPermissaoAplicacao
{
    public int TotalPapeisAtivos { get; set; }
    public int TotalPapeisInativos { get; set; }
    public int TotalUsuariosComPermissao { get; set; }
    public DateTime? UltimaAtribuicao { get; set; }
    public DateTime? UltimoUso { get; set; }
    public int TotalUsosUltimo30Dias { get; set; }
}