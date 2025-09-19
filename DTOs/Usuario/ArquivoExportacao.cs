using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Usuario;

/// <summary>
/// Arquivo de exportação
/// </summary>
public class ArquivoExportacao
{
    public string NomeArquivo { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string Conteudo { get; set; } = string.Empty; // Base64
    public long Tamanho { get; set; }
    public DateTime GeradoEm { get; set; } = DateTime.UtcNow;
}