using Gestus.Dados;
using Gestus.Modelos;
using Gestus.DTOs.Notificacao;
using Gestus.DTOs.Comuns;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gestus.Services;

/// <summary>
/// Serviço para gerenciamento de notificações
/// </summary>
public class NotificacaoService : INotificacaoService
{
    private readonly GestusDbContexto _context;
    private readonly ILogger<NotificacaoService> _logger;

    public NotificacaoService(
        GestusDbContexto context,
        ILogger<NotificacaoService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Criar uma nova notificação para um usuário
    /// </summary>
    public async Task<NotificacaoDTO> CriarNotificacaoAsync(CriarNotificacaoDTO dto)
    {
        try
        {
            var notificacao = new Notificacao
            {
                Id = Guid.NewGuid(),
                UsuarioId = dto.UsuarioId,
                Titulo = dto.Titulo,
                Mensagem = dto.Mensagem,
                Tipo = dto.Tipo,
                Icone = dto.Icone,
                Cor = dto.Cor,
                Origem = dto.Origem,
                Prioridade = dto.Prioridade,
                DataExpiracao = dto.DataExpiracao,
                EnviarEmail = dto.EnviarEmail,
                DadosAdicionais = dto.DadosAdicionais,
                DataCriacao = DateTime.UtcNow,
                Lida = false
            };

            await _context.Notificacoes.AddAsync(notificacao);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Notificação criada com sucesso. Id: {NotificacaoId}, Usuario: {UsuarioId}", 
                notificacao.Id, notificacao.UsuarioId);

            return MapearParaDTO(notificacao);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar notificação para usuário {UsuarioId}", dto.UsuarioId);
            throw;
        }
    }

    /// <summary>
    /// Criar notificações para múltiplos usuários (broadcast)
    /// </summary>
    public async Task<List<NotificacaoDTO>> CriarNotificacaoBroadcastAsync(CriarNotificacaoBroadcastDTO dto)
    {
        try
        {
            var notificacoes = new List<Notificacao>();
            var dataAgora = DateTime.UtcNow;

            foreach (var usuarioId in dto.UsuarioIds)
            {
                var notificacao = new Notificacao
                {
                    Id = Guid.NewGuid(),
                    UsuarioId = usuarioId,
                    Titulo = dto.Titulo,
                    Mensagem = dto.Mensagem,
                    Tipo = dto.Tipo,
                    Icone = dto.Icone,
                    Cor = dto.Cor,
                    Origem = dto.Origem,
                    Prioridade = dto.Prioridade,
                    DataExpiracao = dto.DataExpiracao,
                    EnviarEmail = dto.EnviarEmail,
                    DadosAdicionais = dto.DadosAdicionais,
                    DataCriacao = dataAgora,
                    Lida = false
                };

                notificacoes.Add(notificacao);
            }

            await _context.Notificacoes.AddRangeAsync(notificacoes);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Notificações broadcast criadas com sucesso. Quantidade: {Quantidade}", 
                notificacoes.Count);

            return notificacoes.Select(MapearParaDTO).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar notificações broadcast");
            throw;
        }
    }

    /// <summary>
    /// Obter notificações de um usuário com filtros
    /// </summary>
    public async Task<RespostaPaginada<NotificacaoDTO>> ObterNotificacoesUsuarioAsync(int usuarioId, FiltrarNotificacoesDTO filtros)
    {
        try
        {
            var query = _context.Notificacoes
                .Where(n => n.UsuarioId == usuarioId);

            // Aplicar filtros
            if (filtros.ApenasNaoLidas.HasValue && filtros.ApenasNaoLidas.Value)
            {
                query = query.Where(n => !n.Lida);
            }

            if (filtros.Tipos?.Any() == true)
            {
                query = query.Where(n => filtros.Tipos.Contains(n.Tipo));
            }

            if (filtros.Prioridades?.Any() == true)
            {
                query = query.Where(n => filtros.Prioridades.Contains(n.Prioridade));
            }

            if (filtros.Origens?.Any() == true)
            {
                query = query.Where(n => filtros.Origens.Contains(n.Origem));
            }

            if (filtros.DataInicio.HasValue)
            {
                query = query.Where(n => n.DataCriacao >= filtros.DataInicio.Value);
            }

            if (filtros.DataFim.HasValue)
            {
                query = query.Where(n => n.DataCriacao <= filtros.DataFim.Value);
            }

            if (!string.IsNullOrWhiteSpace(filtros.TextoPesquisa))
            {
                var texto = filtros.TextoPesquisa.ToLower();
                query = query.Where(n => n.Titulo.Contains(texto) || 
                                        n.Mensagem.Contains(texto));
            }

            // Ordenação
            query = filtros.OrdenarPor?.ToLower() switch
            {
                "titulo" => filtros.OrdemDecrescente ? 
                    query.OrderByDescending(n => n.Titulo) : 
                    query.OrderBy(n => n.Titulo),
                "tipo" => filtros.OrdemDecrescente ? 
                    query.OrderByDescending(n => n.Tipo) : 
                    query.OrderBy(n => n.Tipo),
                "prioridade" => filtros.OrdemDecrescente ? 
                    query.OrderByDescending(n => n.Prioridade) : 
                    query.OrderBy(n => n.Prioridade),
                _ => filtros.OrdemDecrescente ? 
                    query.OrderByDescending(n => n.DataCriacao) : 
                    query.OrderBy(n => n.DataCriacao)
            };

            // Contagem total
            var totalRegistros = await query.CountAsync();

            // Paginação
            var notificacoes = await query
                .Skip((filtros.Pagina - 1) * filtros.ItensPorPagina)
                .Take(filtros.ItensPorPagina)
                .ToListAsync();

            var notificacoesDTO = notificacoes.Select(MapearParaDTO).ToList();

            return new RespostaPaginada<NotificacaoDTO>
            {
                Dados = notificacoesDTO,
                TotalItens = totalRegistros,
                PaginaAtual = filtros.Pagina,
                ItensPorPagina = filtros.ItensPorPagina,
                TotalPaginas = (int)Math.Ceiling((double)totalRegistros / filtros.ItensPorPagina),
                TemProximaPagina = filtros.Pagina < (int)Math.Ceiling((double)totalRegistros / filtros.ItensPorPagina),
                TemPaginaAnterior = filtros.Pagina > 1
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter notificações do usuário {UsuarioId}", usuarioId);
            throw;
        }
    }

    /// <summary>
    /// Obter uma notificação específica
    /// </summary>
    public async Task<NotificacaoDTO?> ObterNotificacaoPorIdAsync(Guid notificacaoId, int usuarioId)
    {
        try
        {
            var notificacao = await _context.Notificacoes
                .FirstOrDefaultAsync(n => n.Id == notificacaoId && n.UsuarioId == usuarioId);

            return notificacao != null ? MapearParaDTO(notificacao) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter notificação {NotificacaoId} do usuário {UsuarioId}", 
                notificacaoId, usuarioId);
            throw;
        }
    }

    /// <summary>
    /// Marcar uma notificação como lida
    /// </summary>
    public async Task<bool> MarcarComoLidaAsync(Guid notificacaoId, int usuarioId)
    {
        try
        {
            var notificacao = await _context.Notificacoes
                .FirstOrDefaultAsync(n => n.Id == notificacaoId && n.UsuarioId == usuarioId);

            if (notificacao == null)
                return false;

            if (!notificacao.Lida)
            {
                notificacao.Lida = true;
                notificacao.DataLeitura = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Notificação marcada como lida. Id: {NotificacaoId}", notificacaoId);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao marcar notificação {NotificacaoId} como lida", notificacaoId);
            throw;
        }
    }

    /// <summary>
    /// Marcar todas as notificações de um usuário como lidas
    /// </summary>
    public async Task<int> MarcarTodasComoLidasAsync(int usuarioId)
    {
        try
        {
            var notificacoesNaoLidas = await _context.Notificacoes
                .Where(n => n.UsuarioId == usuarioId && !n.Lida)
                .ToListAsync();

            var dataAgora = DateTime.UtcNow;
            var contador = 0;

            foreach (var notificacao in notificacoesNaoLidas)
            {
                notificacao.Lida = true;
                notificacao.DataLeitura = dataAgora;
                contador++;
            }

            if (contador > 0)
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Todas as notificações do usuário {UsuarioId} marcadas como lidas. Quantidade: {Quantidade}", 
                    usuarioId, contador);
            }

            return contador;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao marcar todas as notificações do usuário {UsuarioId} como lidas", usuarioId);
            throw;
        }
    }

    /// <summary>
    /// Excluir uma notificação
    /// </summary>
    public async Task<bool> ExcluirNotificacaoAsync(Guid notificacaoId, int usuarioId)
    {
        try
        {
            var notificacao = await _context.Notificacoes
                .FirstOrDefaultAsync(n => n.Id == notificacaoId && n.UsuarioId == usuarioId);

            if (notificacao == null)
                return false;

            _context.Notificacoes.Remove(notificacao);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Notificação excluída com sucesso. Id: {NotificacaoId}", notificacaoId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao excluir notificação {NotificacaoId}", notificacaoId);
            throw;
        }
    }

    /// <summary>
    /// Obter contagem de notificações não lidas de um usuário
    /// </summary>
    public async Task<int> ObterContagemNaoLidasAsync(int usuarioId)
    {
        try
        {
            return await _context.Notificacoes
                .CountAsync(n => n.UsuarioId == usuarioId && !n.Lida);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter contagem de notificações não lidas do usuário {UsuarioId}", usuarioId);
            throw;
        }
    }

    /// <summary>
    /// Limpar notificações antigas/expiradas
    /// </summary>
    public async Task<int> LimparNotificacoesAntigasAsync(int diasRetencao = 90)
    {
        try
        {
            var dataLimite = DateTime.UtcNow.AddDays(-diasRetencao);
            
            var notificacoesAntigas = await _context.Notificacoes
                .Where(n => n.DataCriacao < dataLimite || 
                           (n.DataExpiracao.HasValue && n.DataExpiracao.Value < DateTime.UtcNow))
                .ToListAsync();

            var contador = notificacoesAntigas.Count;

            if (contador > 0)
            {
                _context.Notificacoes.RemoveRange(notificacoesAntigas);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Notificações antigas limpas com sucesso. Quantidade: {Quantidade}", contador);
            }

            return contador;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao limpar notificações antigas");
            throw;
        }
    }

    /// <summary>
    /// Criar notificação automática de login bem-sucedido
    /// </summary>
    public async Task CriarNotificacaoLoginSucessoAsync(int usuarioId, string enderecoIp, string userAgent)
    {
        var dto = new CriarNotificacaoDTO
        {
            UsuarioId = usuarioId,
            Titulo = "Login realizado com sucesso",
            Mensagem = $"Seu login foi realizado com sucesso em {DateTime.Now:dd/MM/yyyy HH:mm}.",
            Tipo = "login_sucesso",
            Cor = "success",
            Origem = "Segurança",
            Prioridade = 3,
            DadosAdicionais = System.Text.Json.JsonSerializer.Serialize(new { 
                EnderecoIp = enderecoIp,
                UserAgent = userAgent,
                DataHora = DateTime.UtcNow
            })
        };

        await CriarNotificacaoAsync(dto);
    }

    /// <summary>
    /// Criar notificação automática de login falhado
    /// </summary>
    public async Task CriarNotificacaoLoginFalhadoAsync(int usuarioId, string enderecoIp, string motivoFalha)
    {
        var dto = new CriarNotificacaoDTO
        {
            UsuarioId = usuarioId,
            Titulo = "Tentativa de login falhada",
            Mensagem = $"Houve uma tentativa de login em sua conta em {DateTime.Now:dd/MM/yyyy HH:mm}. Motivo: {motivoFalha}",
            Tipo = "login_falhou",
            Cor = "error",
            Origem = "Segurança",
            Prioridade = 1,
            DadosAdicionais = System.Text.Json.JsonSerializer.Serialize(new { 
                EnderecoIp = enderecoIp,
                MotivoFalha = motivoFalha,
                DataHora = DateTime.UtcNow
            })
        };

        await CriarNotificacaoAsync(dto);
    }

    /// <summary>
    /// Criar notificação automática de alteração de senha
    /// </summary>
    public async Task CriarNotificacaoAlteracaoSenhaAsync(int usuarioId, string enderecoIp)
    {
        var dto = new CriarNotificacaoDTO
        {
            UsuarioId = usuarioId,
            Titulo = "Senha alterada com sucesso",
            Mensagem = $"Sua senha foi alterada com sucesso em {DateTime.Now:dd/MM/yyyy HH:mm}.",
            Tipo = "alteracao_senha",
            Cor = "warning",
            Origem = "Segurança",
            Prioridade = 2,
            DadosAdicionais = System.Text.Json.JsonSerializer.Serialize(new { 
                EnderecoIp = enderecoIp,
                DataHora = DateTime.UtcNow
            })
        };

        await CriarNotificacaoAsync(dto);
    }

    /// <summary>
    /// Criar notificação automática de alteração de dados pessoais
    /// </summary>
    public async Task CriarNotificacaoAlteracaoDadosAsync(int usuarioId, string camposAlterados)
    {
        var dto = new CriarNotificacaoDTO
        {
            UsuarioId = usuarioId,
            Titulo = "Dados pessoais alterados",
            Mensagem = $"Seus dados pessoais foram alterados em {DateTime.Now:dd/MM/yyyy HH:mm}. Campos alterados: {camposAlterados}",
            Tipo = "dados_alterados",
            Cor = "info",
            Origem = "Sistema",
            Prioridade = 3,
            DadosAdicionais = System.Text.Json.JsonSerializer.Serialize(new {
                CamposAlterados = camposAlterados,
                DataHora = DateTime.UtcNow
            })
        };

        await CriarNotificacaoAsync(dto);
    }

    /// <summary>
    /// Criar notificação automática de novo usuário no sistema
    /// </summary>
    public async Task CriarNotificacaoNovoUsuarioAsync(int usuarioIdCriado, string nomeUsuario, int usuarioIdAdmin)
    {
        var dto = new CriarNotificacaoDTO
        {
            UsuarioId = usuarioIdAdmin,
            Titulo = "Novo usuário cadastrado",
            Mensagem = $"Um novo usuário '{nomeUsuario}' foi cadastrado no sistema em {DateTime.Now:dd/MM/yyyy HH:mm}.",
            Tipo = "novo_usuario",
            Cor = "info",
            Origem = "Sistema",
            Prioridade = 3,
            DadosAdicionais = System.Text.Json.JsonSerializer.Serialize(new {
                UsuarioIdCriado = usuarioIdCriado,
                NomeUsuario = nomeUsuario,
                DataHora = DateTime.UtcNow
            })
        };

        await CriarNotificacaoAsync(dto);
    }

    /// <summary>
    /// Criar notificação automática de alteração de permissões
    /// </summary>
    public async Task CriarNotificacaoAlteracaoPermissoesAsync(int usuarioId, string alteracoes, int usuarioIdAdmin)
    {
        var dto = new CriarNotificacaoDTO
        {
            UsuarioId = usuarioId,
            Titulo = "Permissões alteradas",
            Mensagem = $"Suas permissões foram alteradas em {DateTime.Now:dd/MM/yyyy HH:mm}. Alterações: {alteracoes}",
            Tipo = "permissoes_alteradas",
            Cor = "warning",
            Origem = "Sistema",
            Prioridade = 2,
            DadosAdicionais = System.Text.Json.JsonSerializer.Serialize(new {
                Alteracoes = alteracoes,
                UsuarioIdAdmin = usuarioIdAdmin,
                DataHora = DateTime.UtcNow
            })
        };

        await CriarNotificacaoAsync(dto);
    }

    /// <summary>
    /// Mapear modelo para DTO
    /// </summary>
    private static NotificacaoDTO MapearParaDTO(Notificacao notificacao)
    {
        return new NotificacaoDTO
        {
            Id = notificacao.Id,
            UsuarioId = notificacao.UsuarioId,
            Titulo = notificacao.Titulo,
            Mensagem = notificacao.Mensagem,
            Tipo = notificacao.Tipo,
            Icone = notificacao.Icone,
            Cor = notificacao.Cor,
            Prioridade = notificacao.Prioridade,
            Origem = notificacao.Origem,
            DataCriacao = notificacao.DataCriacao,
            DataLeitura = notificacao.DataLeitura,
            DataExpiracao = notificacao.DataExpiracao,
            EnviarEmail = notificacao.EnviarEmail,
            EmailEnviado = notificacao.EmailEnviado,
            DadosAdicionais = notificacao.DadosAdicionais,
            Lida = notificacao.Lida,
            IdadeTexto = notificacao.ObterIdadeTexto()
        };
    }
}