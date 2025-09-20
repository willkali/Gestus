using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Grupo;

/// <summary>
/// Resposta do gerenciamento de usuários no grupo
/// </summary>
public class RespostaGerenciamentoUsuarios
{
    public bool Sucesso { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public int TotalUsuarios { get; set; }
    public int UsuariosAdicionados { get; set; }
    public int UsuariosRemovidos { get; set; }
    public List<string> Detalhes { get; set; } = new();
    public DateTime DataOperacao { get; set; } = DateTime.UtcNow;
}