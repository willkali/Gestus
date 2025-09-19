using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Usuario;

/// <summary>
/// Status de operação em lote
/// </summary>
public class StatusOperacaoLote
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Pendente, Executando, Concluido, Erro, Cancelado
    public int Progresso { get; set; } // 0-100
    
    public DateTime IniciadoEm { get; set; }
    public DateTime? ConcluidoEm { get; set; }
    
    public int TotalItens { get; set; }
    public int ItensProcessados { get; set; }
    public int ItensSucesso { get; set; }
    public int ItensErro { get; set; }
    
    public TimeSpan? TempoEstimadoRestante { get; set; }
    public string Mensagem { get; set; } = string.Empty;
}