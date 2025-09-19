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
    public string? Observacoes { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
    public DateTime? UltimoLogin { get; set; }
    
    public List<PapelUsuario> Papeis { get; set; } = new();
    public List<PermissaoUsuario> Permissoes { get; set; } = new();
    public List<GrupoUsuario> Grupos { get; set; } = new();
    public EstatisticasUsuario Estatisticas { get; set; } = new();
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
    public int ContadorLogins { get; set; }
    public DateTime? UltimoPapelAtribuido { get; set; }
}