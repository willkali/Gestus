namespace Gestus.DTOs.Comuns;

/// <summary>
/// Resultado de exportação de arquivo
/// </summary>
public class ArquivoExportacao
{
    /// <summary>
    /// Nome do arquivo gerado
    /// </summary>
    public string NomeArquivo { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de conteúdo MIME
    /// </summary>
    public string TipoConteudo { get; set; } = string.Empty;

    /// <summary>
    /// Tamanho do arquivo em bytes
    /// </summary>
    public long Tamanho { get; set; }

    /// <summary>
    /// Conteúdo do arquivo em Base64
    /// </summary>
    public string ConteudoBase64 { get; set; } = string.Empty;

    /// <summary>
    /// Data de geração do arquivo
    /// </summary>
    public DateTime DataGeracao { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Hash MD5 do arquivo para verificação
    /// </summary>
    public string? HashMd5 { get; set; }

    /// <summary>
    /// Metadados adicionais do arquivo (JSON)
    /// </summary>
    public string? Metadados { get; set; }
}