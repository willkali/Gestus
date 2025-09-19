using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Usuario;

/// <summary>
/// Item processado na operação em lote
/// </summary>
public class ItemProcessado
{
    public string Id { get; set; } = string.Empty;
    public string Identificador { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Sucesso, Erro, Ignorado
    public string Mensagem { get; set; } = string.Empty;
    public DateTime ProcessadoEm { get; set; } = DateTime.UtcNow;
}