using Gestus.DTOs.Notificacao;
using Gestus.DTOs.Comuns;

namespace Gestus.Services;

/// <summary>
/// Interface para o serviço de notificações
/// </summary>
public interface INotificacaoService
{
    /// <summary>
    /// Criar uma nova notificação para um usuário
    /// </summary>
    Task<NotificacaoDTO> CriarNotificacaoAsync(CriarNotificacaoDTO dto);

    /// <summary>
    /// Criar notificações para múltiplos usuários (broadcast)
    /// </summary>
    Task<List<NotificacaoDTO>> CriarNotificacaoBroadcastAsync(CriarNotificacaoBroadcastDTO dto);

    /// <summary>
    /// Obter notificações de um usuário com filtros
    /// </summary>
    Task<RespostaPaginada<NotificacaoDTO>> ObterNotificacoesUsuarioAsync(int usuarioId, FiltrarNotificacoesDTO filtros);

    /// <summary>
    /// Obter uma notificação específica
    /// </summary>
    Task<NotificacaoDTO?> ObterNotificacaoPorIdAsync(Guid notificacaoId, int usuarioId);

    /// <summary>
    /// Marcar uma notificação como lida
    /// </summary>
    Task<bool> MarcarComoLidaAsync(Guid notificacaoId, int usuarioId);

    /// <summary>
    /// Marcar todas as notificações de um usuário como lidas
    /// </summary>
    Task<int> MarcarTodasComoLidasAsync(int usuarioId);

    /// <summary>
    /// Excluir uma notificação
    /// </summary>
    Task<bool> ExcluirNotificacaoAsync(Guid notificacaoId, int usuarioId);

    /// <summary>
    /// Obter contagem de notificações não lidas de um usuário
    /// </summary>
    Task<int> ObterContagemNaoLidasAsync(int usuarioId);

    /// <summary>
    /// Limpar notificações antigas/expiradas
    /// </summary>
    Task<int> LimparNotificacoesAntigasAsync(int diasRetencao = 90);

    /// <summary>
    /// Criar notificação automática de login bem-sucedido
    /// </summary>
    Task CriarNotificacaoLoginSucessoAsync(int usuarioId, string enderecoIp, string userAgent);

    /// <summary>
    /// Criar notificação automática de login falhado
    /// </summary>
    Task CriarNotificacaoLoginFalhadoAsync(int usuarioId, string enderecoIp, string motivoFalha);

    /// <summary>
    /// Criar notificação automática de alteração de senha
    /// </summary>
    Task CriarNotificacaoAlteracaoSenhaAsync(int usuarioId, string enderecoIp);

    /// <summary>
    /// Criar notificação automática de alteração de dados pessoais
    /// </summary>
    Task CriarNotificacaoAlteracaoDadosAsync(int usuarioId, string camposAlterados);

    /// <summary>
    /// Criar notificação automática de novo usuário no sistema
    /// </summary>
    Task CriarNotificacaoNovoUsuarioAsync(int usuarioIdCriado, string nomeUsuario, int usuarioIdAdmin);

    /// <summary>
    /// Criar notificação automática de alteração de permissões
    /// </summary>
    Task CriarNotificacaoAlteracaoPermissoesAsync(int usuarioId, string alteracoes, int usuarioIdAdmin);
}