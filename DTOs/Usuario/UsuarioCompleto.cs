namespace Gestus.DTOs.Usuario;

/// <summary>
/// Usuário completo com todos os detalhes
/// </summary>
public class UsuarioCompleto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Sobrenome { get; set; } = string.Empty;
    public string NomeCompleto { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public bool EmailConfirmado { get; set; }
    public bool TelefoneConfirmado { get; set; }
    public bool Ativo { get; set; }
    public bool ContaBloqueada { get; set; }
    public DateTime? DataBloqueio { get; set; }
    public string? MotivoBloqueio { get; set; }
    public string? Observacoes { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
    public DateTime? UltimoLogin { get; set; }
    public int? CriadoPorId { get; set; }
    public string? CriadoPorNome { get; set; }
    public int? AtualizadoPorId { get; set; }
    public string? AtualizadoPorNome { get; set; }
    public string? CaminhoFotoPerfil { get; set; }
    public string? UrlFotoPerfil { get; set; }
    public string? Profissao { get; set; }
    public string? Departamento { get; set; }
    public string? Bio { get; set; }
    public DateTime? DataNascimento { get; set; }
    public string? EnderecoCompleto { get; set; }
    public string? Cidade { get; set; }
    public string? Estado { get; set; }
    public string? Cep { get; set; }
    public string? TelefoneAlternativo { get; set; }
    public string PreferenciaIdioma { get; set; } = "pt-BR";
    public string PreferenciaTimezone { get; set; } = "America/Sao_Paulo";
    public ConfiguracaoPrivacidade? Privacidade { get; set; }
    public ConfiguracaoNotificacao? Notificacoes { get; set; }
    public CompletudePerfil? CompletudePerfil { get; set; }
    public int TentativasLoginFalha { get; set; }
    public DateTime? UltimaTentativaLogin { get; set; }
    public bool AutenticacaoDoisFatores { get; set; }
    public bool AutenticacaoDoisFatoresAtiva { get; set; }
    public List<PapelUsuario> Papeis { get; set; } = new();
    public List<PermissaoUsuario> Permissoes { get; set; } = new();
    public List<GrupoUsuario> Grupos { get; set; } = new();
    public List<AplicacaoUsuario> Aplicacoes { get; set; } = new();
    public EstatisticasUsuario Estatisticas { get; set; } = new();
    public HistoricoUsuario? HistoricoRecente { get; set; }
    public InformacoesAdministrativas? InformacoesAdmin { get; set; }
}

public class PapelUsuario
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string? Categoria { get; set; }
    public DateTime DataAtribuicao { get; set; }
    public DateTime? DataExpiracao { get; set; }
}

public class PermissaoUsuario
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Recurso { get; set; } = string.Empty;
    public string Acao { get; set; } = string.Empty;
    public string? Categoria { get; set; }
    public string OrigemPapel { get; set; } = string.Empty;
}

public class GrupoUsuario
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string? Tipo { get; set; }
    public DateTime DataAdesao { get; set; }
}

public class EstatisticasUsuario
{
    public int TotalPapeis { get; set; }
    public int TotalPermissoes { get; set; }
    public int TotalGrupos { get; set; }
    public int TotalAplicacoes { get; set; }
    public int AplicacoesAprovadas { get; set; }
    public int AplicacoesPendentes { get; set; }
    public int AplicacoesExpiradas { get; set; }
    public int AplicacoesSuspensas { get; set; }
    public int ContadorLogins { get; set; }
    public int ContadorLoginsFalha { get; set; }
    public DateTime? UltimoPapelAtribuido { get; set; }
    public DateTime? UltimaAplicacaoSolicitada { get; set; }
    public DateTime? UltimaAlteracaoSenha { get; set; }
    public TimeSpan? TempoMedioSessao { get; set; }
    public int SessoesAtivas { get; set; }
    public int TotalNotificacoes { get; set; }
    public int NotificacoesNaoLidas { get; set; }
}

public class HistoricoUsuario
{
    public List<EventoUsuario> UltimosLogins { get; set; } = new();
    public List<EventoUsuario> UltimasAlteracoes { get; set; } = new();
    public List<EventoUsuario> UltimasSolicitacoes { get; set; } = new();
    public List<EventoUsuario> UltimosAcessos { get; set; } = new();
}

public class EventoUsuario
{
    public DateTime Data { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public bool Sucesso { get; set; }
    public string? EnderecoIp { get; set; }
    public string? UserAgent { get; set; }
}

public class InformacoesAdministrativas
{
    public string StatusGeral { get; set; } = string.Empty;
    public string? UltimaOperacao { get; set; }
    public DateTime? DataUltimaOperacao { get; set; }
    public string? OperadorUltimaOperacao { get; set; }
    public List<string> TagsAdministrativas { get; set; } = new();
    public int NivelRisco { get; set; }
    public string? JustificativaNivelRisco { get; set; }
    public bool RequerRevisaoManual { get; set; }
    public DateTime? ProximaRevisao { get; set; }
    public List<string> AlertasAtivos { get; set; } = new();
    public Dictionary<string, object> MetadadosAdicionais { get; set; } = new();
}