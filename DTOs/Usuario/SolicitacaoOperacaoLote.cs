using System.ComponentModel.DataAnnotations;

namespace Gestus.DTOs.Usuario;

/// <summary>
/// Solicitação para operação em lote
/// </summary>
public class SolicitacaoOperacaoLote
{
    /// <summary>
    /// Tipo de operação: ativar, desativar, excluir, atribuir-papeis, remover-papeis, exportar, criar, atualizar
    /// </summary>
    [Required(ErrorMessage = "Tipo de operação é obrigatório")]
    public string TipoOperacao { get; set; } = string.Empty;

    /// <summary>
    /// Lista de IDs dos usuários (para operações em usuários existentes)
    /// </summary>
    public List<int>? UsuariosIds { get; set; }

    /// <summary>
    /// Dados de usuários para criação/atualização em lote
    /// </summary>
    public List<DadosUsuarioLote>? DadosUsuarios { get; set; }

    /// <summary>
    /// Parâmetros específicos da operação
    /// </summary>
    public Dictionary<string, string>? ParametrosOperacao { get; set; }

    /// <summary>
    /// Executar de forma assíncrona (para operações grandes)
    /// </summary>
    public bool ExecutarAssincrono { get; set; } = false;

    /// <summary>
    /// Notificar por email quando concluído
    /// </summary>
    public bool NotificarConclusao { get; set; } = false;
}