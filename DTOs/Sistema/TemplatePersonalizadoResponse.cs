using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Sistema;

/// <summary>
/// Resposta do template personalizado
/// </summary>
public class TemplatePersonalizadoResponse
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Assunto { get; set; } = string.Empty;
    public string CorpoHtml { get; set; } = string.Empty;
    public string? CorpoTexto { get; set; }
    public string? Descricao { get; set; }
    public bool Ativo { get; set; }
    public bool IsTemplate { get; set; }
    public string CriadoPor { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
    public string? AtualizadoPor { get; set; }
    
    /// <summary>
    /// Variáveis obrigatórias encontradas no template
    /// </summary>
    public List<string> VariaveisObrigatoriasEncontradas { get; set; } = new();
    
    /// <summary>
    /// Variáveis obrigatórias faltantes
    /// </summary>
    public List<string> VariaveisObrigatoriasFaltantes { get; set; } = new();
    
    /// <summary>
    /// Variáveis opcionais encontradas
    /// </summary>
    public List<string> VariaveisOpcionaisEncontradas { get; set; } = new();
    
    /// <summary>
    /// Status de validação do template
    /// </summary>
    public bool TemplateValido { get; set; }
    
    /// <summary>
    /// Mensagens de validação
    /// </summary>
    public List<string> MensagensValidacao { get; set; } = new();
}