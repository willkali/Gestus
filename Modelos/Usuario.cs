using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Gestus.Modelos;

public class Usuario : IdentityUser<int>
{
    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Sobrenome { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? NomeCompleto { get; set; }

    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public DateTime? DataAtualizacao { get; set; }
    public DateTime? UltimoLogin { get; set; }
    
    public bool Ativo { get; set; } = true;
    
    [MaxLength(500)]
    public string? Observacoes { get; set; }

    [MaxLength(500)]
    public string? CaminhoFotoPerfil { get; set; }
    
    [MaxLength(100)]
    public string? Profissao { get; set; }
    
    [MaxLength(100)]
    public string? Departamento { get; set; }
    
    [MaxLength(50)]
    public string? PreferenciaIdioma { get; set; } = "pt-BR";
    
    [MaxLength(50)]
    public string? PreferenciaTimezone { get; set; } = "America/Sao_Paulo";
    
    [MaxLength(1000)]
    public string? Bio { get; set; }
    
    public DateTime? DataNascimento { get; set; }
    
    [MaxLength(100)]
    public string? EnderecoCompleto { get; set; }
    
    [MaxLength(50)]
    public string? Cidade { get; set; }
    
    [MaxLength(2)]
    public string? Estado { get; set; }
    
    [MaxLength(10)]
    public string? Cep { get; set; }
    
    [MaxLength(100)]
    public string? TelefoneAlternativo { get; set; }
    
    // Configurações de privacidade
    public bool ExibirEmail { get; set; } = false;
    public bool ExibirTelefone { get; set; } = false;
    public bool ExibirDataNascimento { get; set; } = false;
    public bool ExibirEndereco { get; set; } = false;
    public bool PerfilPublico { get; set; } = false;
    
    // Configurações de notificação
    public bool NotificacaoEmail { get; set; } = true;
    public bool NotificacaoSms { get; set; } = false;
    public bool NotificacaoPush { get; set; } = true;

    // Relacionamentos existentes
    public virtual ICollection<UsuarioPapel> UsuarioPapeis { get; set; } = new List<UsuarioPapel>();
    public virtual ICollection<UsuarioGrupo> UsuarioGrupos { get; set; } = new List<UsuarioGrupo>();
    public virtual ICollection<RegistroAuditoria> RegistrosAuditoria { get; set; } = new List<RegistroAuditoria>();
}