using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Grupo;

/// <summary>
/// Resposta de operação em lote com grupos
/// </summary>
public class RespostaOperacaoLoteGrupos
{
    public bool Sucesso { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public int TotalProcessados { get; set; }
    public int TotalSucesso { get; set; }
    public int TotalErros { get; set; }
    public List<string> Detalhes { get; set; } = new();
    public DateTime DataOperacao { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Tempo total de processamento
    /// </summary>
    public TimeSpan TempoProcessamento { get; set; }
    
    /// <summary>
    /// Grupos que falharam no processamento
    /// </summary>
    public List<int> GruposFalharam { get; set; } = new();
}