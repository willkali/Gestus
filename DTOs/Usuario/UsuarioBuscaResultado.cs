using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Usuario;

/// <summary>
/// Resultado individual da busca
/// </summary>
public class UsuarioBuscaResultado
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Sobrenome { get; set; } = string.Empty;
    public string NomeCompleto { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public bool Ativo { get; set; }
    public bool EmailConfirmado { get; set; }
    public bool TelefoneConfirmado { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
    public DateTime? UltimoLogin { get; set; }
    public List<PapelBusca> Papeis { get; set; } = new();
    public List<GrupoBusca> Grupos { get; set; } = new();
    public EstatisticasBusca Estatisticas { get; set; } = new();
}