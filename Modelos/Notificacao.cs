using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gestus.Modelos;

/// <summary>
/// Representa uma notificação do sistema para um usuário
/// </summary>
public class Notificacao
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// ID do usuário que deve receber a notificação
    /// </summary>
    [Required]
    public int UsuarioId { get; set; }

    /// <summary>
    /// Tipo da notificação (login_sucesso, alteracao_senha, etc.)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Tipo { get; set; } = string.Empty;

    /// <summary>
    /// Título da notificação
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Titulo { get; set; } = string.Empty;

    /// <summary>
    /// Mensagem detalhada da notificação
    /// </summary>
    [Required]
    [StringLength(1000)]
    public string Mensagem { get; set; } = string.Empty;

    /// <summary>
    /// Ícone da notificação (emoji ou classe CSS)
    /// </summary>
    [StringLength(50)]
    public string? Icone { get; set; }

    /// <summary>
    /// Cor/categoria da notificação (sucesso, erro, aviso, info)
    /// </summary>
    [Required]
    [StringLength(20)]
    public string Cor { get; set; } = "info";

    /// <summary>
    /// Indica se a notificação foi lida
    /// </summary>
    public bool Lida { get; set; } = false;

    /// <summary>
    /// Data e hora da criação da notificação
    /// </summary>
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Data e hora em que a notificação foi lida
    /// </summary>
    public DateTime? DataLeitura { get; set; }

    /// <summary>
    /// Origem da notificação (Sistema, Segurança, etc.)
    /// </summary>
    [StringLength(100)]
    public string? Origem { get; set; } = "Sistema";

    /// <summary>
    /// Prioridade da notificação (1=Alta, 2=Média, 3=Baixa)
    /// </summary>
    public int Prioridade { get; set; } = 2;

    /// <summary>
    /// Data de expiração da notificação (opcional)
    /// </summary>
    public DateTime? DataExpiracao { get; set; }

    /// <summary>
    /// Indica se a notificação deve ser enviada por email
    /// </summary>
    public bool EnviarEmail { get; set; } = false;

    /// <summary>
    /// Indica se o email já foi enviado
    /// </summary>
    public bool EmailEnviado { get; set; } = false;

    /// <summary>
    /// Dados adicionais em JSON (opcional)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? DadosAdicionais { get; set; }

    /// <summary>
    /// Referência para o usuário
    /// </summary>
    [ForeignKey(nameof(UsuarioId))]
    public virtual Usuario Usuario { get; set; } = null!;

    /// <summary>
    /// Marcar a notificação como lida
    /// </summary>
    public void MarcarComoLida()
    {
        if (!Lida)
        {
            Lida = true;
            DataLeitura = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Verificar se a notificação está expirada
    /// </summary>
    public bool EstaExpirada()
    {
        return DataExpiracao.HasValue && DateTime.UtcNow > DataExpiracao.Value;
    }

    /// <summary>
    /// Obter a idade da notificação em texto legível
    /// </summary>
    public string ObterIdadeTexto()
    {
        var diferenca = DateTime.UtcNow - DataCriacao;
        
        if (diferenca.TotalDays >= 1)
        {
            var dias = (int)diferenca.TotalDays;
            return $"{dias} dia{(dias > 1 ? "s" : "")} atrás";
        }
        else if (diferenca.TotalHours >= 1)
        {
            var horas = (int)diferenca.TotalHours;
            return $"{horas} hora{(horas > 1 ? "s" : "")} atrás";
        }
        else if (diferenca.TotalMinutes >= 1)
        {
            var minutos = (int)diferenca.TotalMinutes;
            return $"{minutos} minuto{(minutos > 1 ? "s" : "")} atrás";
        }
        else
        {
            return "Agora mesmo";
        }
    }
}