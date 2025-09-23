using System.ComponentModel.DataAnnotations;
using Gestus.DTOs.Comuns; // ✅ Adicionado using

namespace Gestus.DTOs.Usuario;

/// <summary>
/// Resposta de operação em lote
/// </summary>
public class RespostaOperacaoLote
{
    public bool Sucesso { get; set; }
    public string TipoOperacao { get; set; } = string.Empty;
    public string JobId { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    
    public DateTime IniciadoEm { get; set; }
    public DateTime? ConcluidoEm { get; set; }
    
    public int TotalProcessados { get; set; }
    public int TotalSucessos { get; set; }
    public int TotalErros { get; set; }
    public int TotalIgnorados { get; set; }
    
    public List<ItemProcessado> ItensProcessados { get; set; } = new();
    
    public ArquivoExportacao? ArquivoExportacao { get; set; }
    
    public TimeSpan TempoExecucao => ConcluidoEm.HasValue ? ConcluidoEm.Value - IniciadoEm : TimeSpan.Zero;
}