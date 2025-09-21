namespace Gestus.DTOs.Auditoria;

/// <summary>
/// Informações resumidas do usuário para auditoria
/// </summary>
public class UsuarioAuditoria
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Sobrenome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string NomeCompleto => $"{Nome} {Sobrenome}".Trim();
}