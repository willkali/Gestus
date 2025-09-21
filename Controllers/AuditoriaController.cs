using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using Gestus.Modelos;
using Gestus.Dados;
using Gestus.DTOs.Auditoria;
using Gestus.DTOs.Comuns;
using Gestus.Validadores;
using System.Text;
using ClosedXML.Excel;

namespace Gestus.Controllers;

/// <summary>
/// Controller para consulta e análise de registros de auditoria
/// Fornece endpoints para rastreabilidade, compliance e análise de atividades
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class AuditoriaController : ControllerBase
{
    private readonly GestusDbContexto _context;
    private readonly ILogger<AuditoriaController> _logger;

    public AuditoriaController(
        GestusDbContexto context,
        ILogger<AuditoriaController> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Operações Básicas de Consulta

    /// <summary>
    /// Lista registros de auditoria com filtros e paginação
    /// </summary>
    /// <param name="filtros">Filtros para consulta</param>
    /// <returns>Lista paginada de registros de auditoria</returns>
    [HttpGet]
    [ProducesResponseType(typeof(RespostaPaginada<RegistroAuditoriaDetalhado>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ListarRegistros([FromQuery] FiltrosAuditoria filtros)
    {
        try
        {
            if (!TemPermissao("Auditoria.Visualizar"))
            {
                return Forbid();
            }

            var validator = new FiltrosAuditoriaValidator();
            var validationResult = validator.Validate(filtros);
            if (!validationResult.IsValid)
            {
                return BadRequest(new RespostaErro 
                { 
                    Erro = "Filtros inválidos", 
                    Mensagem = "Os filtros fornecidos são inválidos",
                    Detalhes = validationResult.Errors.Select(e => e.ErrorMessage).ToList()
                });
            }

            var query = _context.RegistrosAuditoria
                .Include(r => r.Usuario)
                .AsQueryable();

            query = AplicarFiltros(query, filtros);
            query = AplicarOrdenacao(query, filtros.OrdenarPor, filtros.DirecaoOrdenacao);

            var totalItens = await query.CountAsync();

            // ✅ CORRIGIDO: Carregar dados básicos primeiro
            var registrosBasicos = await query
                .Skip((filtros.Pagina - 1) * filtros.ItensPorPagina)
                .Take(filtros.ItensPorPagina)
                .Select(r => new
                {
                    r.Id,
                    r.Acao,
                    r.Recurso,
                    r.RecursoId,
                    r.Observacoes,
                    r.DataHora,
                    r.EnderecoIp,
                    r.UserAgent,
                    r.DadosAntes,
                    r.DadosDepois,
                    Usuario = r.Usuario
                })
                .ToListAsync();

            // ✅ CORRIGIDO: Processar JSON de forma segura em memória
            var registrosDetalhados = registrosBasicos.Select(r => new RegistroAuditoriaDetalhado
            {
                Id = r.Id,
                Acao = r.Acao,
                Recurso = r.Recurso,
                RecursoId = r.RecursoId,
                Observacoes = r.Observacoes,
                DataHora = r.DataHora,
                EnderecoIp = r.EnderecoIp,
                UserAgent = r.UserAgent,
                Usuario = r.Usuario != null ? new UsuarioAuditoria
                {
                    Id = r.Usuario.Id,
                    Nome = r.Usuario.Nome,
                    Sobrenome = r.Usuario.Sobrenome,
                    Email = r.Usuario.Email ?? string.Empty
                } : null,
                DadosAntes = ProcessarDadosJsonSeguro(r.DadosAntes),
                DadosDepois = ProcessarDadosJsonSeguro(r.DadosDepois),
                Alteracoes = GerarResumoAlteracoesSeguro(r.DadosAntes, r.DadosDepois),
                CategoriaAcao = ObterCategoriaAcao(r.Acao),
                Severidade = CalcularSeveridade(r.Acao, r.Recurso),
                Contexto = GerarContextoOperacaoSeguro(r)
            }).ToList();

            var totalPaginas = (int)Math.Ceiling((double)totalItens / filtros.ItensPorPagina);

            var resposta = new RespostaPaginada<RegistroAuditoriaDetalhado>
            {
                Dados = registrosDetalhados,
                PaginaAtual = filtros.Pagina,
                ItensPorPagina = filtros.ItensPorPagina,
                TotalItens = totalItens,
                TotalPaginas = totalPaginas,
                TemProximaPagina = filtros.Pagina < totalPaginas,
                TemPaginaAnterior = filtros.Pagina > 1
            };

            return Ok(resposta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar registros de auditoria");
            return StatusCode(500, new RespostaErro { Erro = "Erro interno", Mensagem = "Erro ao consultar registros de auditoria" });
        }
    }

    /// <summary>
    /// Processa dados JSON de forma segura
    /// </summary>
    private static object? ProcessarDadosJsonSeguro(string? dadosJson)
    {
        if (string.IsNullOrEmpty(dadosJson))
            return null;

        try
        {
            // Tentar fazer parse do JSON
            return JsonSerializer.Deserialize<object>(dadosJson);
        }
        catch (JsonException)
        {
            // Se JSON for inválido, retornar como string
            return $"[JSON Inválido: {dadosJson.Substring(0, Math.Min(100, dadosJson.Length))}...]";
        }
        catch (Exception)
        {
            // Para qualquer outro erro, retornar indicação de erro
            return "[Dados não disponíveis]";
        }
    }

    /// <summary>
    /// Gera resumo das alterações de forma segura
    /// </summary>
    private List<AlteracaoDetalhada> GerarResumoAlteracoesSeguro(string? dadosAntes, string? dadosDepois)
    {
        var alteracoes = new List<AlteracaoDetalhada>();

        try
        {
            if (string.IsNullOrEmpty(dadosAntes) && string.IsNullOrEmpty(dadosDepois))
                return alteracoes;

            // Tentar processar apenas se os JSONs forem válidos
            JsonElement? elementoAntes = null;
            JsonElement? elementoDepois = null;

            if (!string.IsNullOrEmpty(dadosAntes))
            {
                try
                {
                    elementoAntes = JsonSerializer.Deserialize<JsonElement>(dadosAntes);
                }
                catch (JsonException)
                {
                    alteracoes.Add(new AlteracaoDetalhada
                    {
                        Campo = "DadosAntes",
                        ValorAnterior = "[JSON Inválido]",
                        ValorNovo = null,
                        TipoAlteracao = "Erro"
                    });
                }
            }

            if (!string.IsNullOrEmpty(dadosDepois))
            {
                try
                {
                    elementoDepois = JsonSerializer.Deserialize<JsonElement>(dadosDepois);
                }
                catch (JsonException)
                {
                    alteracoes.Add(new AlteracaoDetalhada
                    {
                        Campo = "DadosDepois",
                        ValorAnterior = null,
                        ValorNovo = "[JSON Inválido]",
                        TipoAlteracao = "Erro"
                    });
                }
            }

            // Se ambos forem válidos, comparar
            if (elementoAntes.HasValue && elementoDepois.HasValue)
            {
                CompararPropriedades(alteracoes, elementoAntes.Value, elementoDepois.Value);
            }
            else if (elementoDepois.HasValue && !elementoAntes.HasValue)
            {
                AdicionarAlteracoesCriacao(alteracoes, elementoDepois.Value);
            }
            else if (elementoAntes.HasValue && !elementoDepois.HasValue)
            {
                AdicionarAlteracoesRemocao(alteracoes, elementoAntes.Value);
            }

            return alteracoes;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao gerar resumo de alterações");
            
            alteracoes.Add(new AlteracaoDetalhada
            {
                Campo = "Sistema",
                ValorAnterior = null,
                ValorNovo = "Erro ao processar alterações",
                TipoAlteracao = "Erro"
            });

            return alteracoes;
        }
    }

    /// <summary>
    /// Gera contexto de operação de forma segura
    /// </summary>
    private Dictionary<string, object> GerarContextoOperacaoSeguro(dynamic registro)
    {
        var contexto = new Dictionary<string, object>();

        try
        {
            if (!string.IsNullOrEmpty(registro.EnderecoIp))
            {
                contexto["TipoIP"] = DeterminarTipoIP(registro.EnderecoIp);
            }

            if (!string.IsNullOrEmpty(registro.UserAgent))
            {
                contexto["Navegador"] = ExtrairNavegador(registro.UserAgent);
                contexto["SistemaOperacional"] = ExtrairSistemaOperacional(registro.UserAgent);
            }

            contexto["HorarioOperacao"] = registro.DataHora.Hour < 8 || registro.DataHora.Hour > 18 ? 
                "Fora do horário comercial" : "Horário comercial";
            contexto["DiaSemana"] = registro.DataHora.DayOfWeek.ToString();

            return contexto;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao gerar contexto da operação");
            return new Dictionary<string, object> { { "Erro", "Não foi possível gerar contexto" } };
        }
    }

    /// <summary>
    /// Obtém detalhes de um registro específico de auditoria
    /// </summary>
    /// <param name="id">ID do registro de auditoria</param>
    /// <returns>Registro de auditoria detalhado</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(RegistroAuditoriaDetalhado), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ObterRegistro(int id)
    {
        try
        {
            if (!TemPermissao("Auditoria.Visualizar"))
            {
                return StatusCode(403, new RespostaErro 
                { 
                    Erro = "Acesso negado", 
                    Mensagem = "Acesso negado para visualizar registros de auditoria" 
                });
            }

            var registro = await _context.RegistrosAuditoria
                .Include(r => r.Usuario)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (registro == null)
            {
                return NotFound(new RespostaErro { Erro = "Não encontrado", Mensagem = "Registro de auditoria não encontrado" });
            }

            var resultado = new RegistroAuditoriaDetalhado
            {
                Id = registro.Id,
                Acao = registro.Acao,
                Recurso = registro.Recurso,
                RecursoId = registro.RecursoId,
                Observacoes = registro.Observacoes,
                DataHora = registro.DataHora,
                EnderecoIp = registro.EnderecoIp,
                UserAgent = registro.UserAgent,
                Usuario = registro.Usuario != null ? new UsuarioAuditoria
                {
                    Id = registro.Usuario.Id,
                    Nome = registro.Usuario.Nome,
                    Sobrenome = registro.Usuario.Sobrenome,
                    Email = registro.Usuario.Email ?? ""
                } : null,
                DadosAntes = ProcessarDadosJson(registro.DadosAntes),
                DadosDepois = ProcessarDadosJson(registro.DadosDepois),
                CategoriaAcao = ObterCategoriaAcao(registro.Acao),
                Severidade = CalcularSeveridade(registro.Acao, registro.Recurso),
                Alteracoes = GerarResumoAlteracoes(registro.DadosAntes, registro.DadosDepois),
                Contexto = GerarContextoOperacao(registro)
            };

            return Ok(resultado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter registro de auditoria {RegistroId}", id);
            return StatusCode(500, new RespostaErro { Erro = "Erro interno", Mensagem = "Erro ao processar solicitação" });
        }
    }

    #endregion

    #region Estatísticas e Métricas

    /// <summary>
    /// Obtém estatísticas gerais de auditoria para um período
    /// </summary>
    /// <param name="dataInicio">Data início do período</param>
    /// <param name="dataFim">Data fim do período</param>
    /// <returns>Estatísticas de auditoria</returns>
    [HttpGet("estatisticas")]
    [ProducesResponseType(typeof(EstatisticasAuditoria), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ObterEstatisticas(
        [FromQuery] DateTime? dataInicio = null,
        [FromQuery] DateTime? dataFim = null)
    {
        try
        {
            if (!TemPermissao("Auditoria.Visualizar"))
            {
                return StatusCode(403, new RespostaErro 
                { 
                    Erro = "Acesso negado", 
                    Mensagem = "Acesso negado para visualizar estatísticas de auditoria" 
                });
            }

            // Definir período padrão (últimos 30 dias)
            var inicio = dataInicio ?? DateTime.UtcNow.AddDays(-30);
            var fim = dataFim ?? DateTime.UtcNow;

            // Validar período
            if ((fim - inicio).TotalDays > 365)
            {
                return BadRequest(new RespostaErro 
                { 
                    Erro = "Período inválido", 
                    Mensagem = "Período não pode exceder 365 dias" 
                });
            }

            var query = _context.RegistrosAuditoria
                .Include(r => r.Usuario)
                .Where(r => r.DataHora >= inicio && r.DataHora <= fim);

            var totalRegistros = await query.CountAsync();

            // Registros por dia
            var registrosPorDia = await query
                .GroupBy(r => r.DataHora.Date)
                .Select(g => new { Data = g.Key, Count = g.Count() })
                .OrderBy(x => x.Data)
                .ToDictionaryAsync(x => x.Data, x => x.Count);

            // Ações mais frequentes
            var acoesMaisFrequentes = await query
                .GroupBy(r => r.Acao)
                .Select(g => new { Acao = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToDictionaryAsync(x => x.Acao, x => x.Count);

            // Recursos mais acessados
            var recursosMaisAcessados = await query
                .GroupBy(r => r.Recurso)
                .Select(g => new { Recurso = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToDictionaryAsync(x => x.Recurso, x => x.Count);

            // Usuários mais ativos
            var usuariosMaisAtivos = await query
                .Where(r => r.Usuario != null)
                .GroupBy(r => new { r.UsuarioId, r.Usuario!.Nome, r.Usuario.Email })
                .Select(g => new UsuarioAtivo
                {
                    UsuarioId = g.Key.UsuarioId!.Value,
                    Nome = g.Key.Nome,
                    Email = g.Key.Email ?? "",
                    TotalOperacoes = g.Count(),
                    UltimaOperacao = g.Max(r => r.DataHora),
                    OperacoesPorTipo = g.GroupBy(r => r.Acao)
                                       .ToDictionary(og => og.Key, og => og.Count())
                })
                .OrderByDescending(x => x.TotalOperacoes)
                .Take(10)
                .ToListAsync();

            // Atividade por hora
            var atividadePorHora = await query
                .GroupBy(r => r.DataHora.Hour)
                .Select(g => new { Hora = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Hora, x => x.Count);

            // IPs mais frequentes
            var ipsMaisFrequentes = await query
                .Where(r => !string.IsNullOrEmpty(r.EnderecoIp))
                .GroupBy(r => r.EnderecoIp!)
                .Select(g => new { IP = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToDictionaryAsync(x => x.IP, x => x.Count);

            // Calcular média e tendência
            var diasPeriodo = Math.Max(1, (fim - inicio).Days);
            var mediaOperacoesPorDia = (decimal)totalRegistros / diasPeriodo;
            var tendencia = CalcularTendenciaAtividade(registrosPorDia);

            var estatisticas = new EstatisticasAuditoria
            {
                TotalRegistros = totalRegistros,
                RegistrosPorDia = registrosPorDia,
                AcoesMaisFrequentes = acoesMaisFrequentes,
                RecursosMaisAcessados = recursosMaisAcessados,
                UsuariosMaisAtivos = usuariosMaisAtivos,
                AtividadePorHora = atividadePorHora,
                IpsMaisFrequentes = ipsMaisFrequentes,
                DataInicio = inicio,
                DataFim = fim,
                MediaOperacoesPorDia = mediaOperacoesPorDia,
                TendenciaAtividade = tendencia
            };

            return Ok(estatisticas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter estatísticas de auditoria");
            return StatusCode(500, new RespostaErro { Erro = "Erro interno", Mensagem = "Erro ao processar solicitação" });
        }
    }

    /// <summary>
    /// Obtém métricas resumidas para dashboard
    /// </summary>
    /// <returns>Métricas principais do sistema</returns>
    [HttpGet("metricas")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ObterMetricas()
    {
        try
        {
            if (!TemPermissao("Auditoria.Visualizar"))
            {
                return StatusCode(403, new RespostaErro 
                { 
                    Erro = "Acesso negado", 
                    Mensagem = "Acesso negado para visualizar métricas de auditoria" 
                });
            }

            var agora = DateTime.UtcNow;
            var ultimasHoras = agora.AddHours(-24);
            var ultimosDias = agora.AddDays(-7);
            var ultimoMes = agora.AddDays(-30);

            // Métricas das últimas 24 horas
            var operacoesUltimas24h = await _context.RegistrosAuditoria
                .CountAsync(r => r.DataHora >= ultimasHoras);

            // Métricas dos últimos 7 dias
            var operacoesUltimos7dias = await _context.RegistrosAuditoria
                .CountAsync(r => r.DataHora >= ultimosDias);

            // Usuários únicos ativos no último mês
            var usuariosAtivosUltimoMes = await _context.RegistrosAuditoria
                .Where(r => r.DataHora >= ultimoMes && r.UsuarioId != null)
                .Select(r => r.UsuarioId)
                .Distinct()
                .CountAsync();

            // Recursos mais alterados no último mês
            var recursosAlterados = await _context.RegistrosAuditoria
                .Where(r => r.DataHora >= ultimoMes)
                .GroupBy(r => r.Recurso)
                .Select(g => new { Recurso = g.Key, Total = g.Count() })
                .OrderByDescending(x => x.Total)
                .Take(5)
                .ToListAsync();

            // Operações de alta criticidade (últimos 7 dias)
            var operacoesCriticas = await _context.RegistrosAuditoria
                .Where(r => r.DataHora >= ultimosDias && 
                           (r.Acao.Contains("Excluir") || r.Acao.Contains("Remover") || 
                            r.Acao.Contains("Desativar") || r.Recurso == "Usuario" || 
                            r.Recurso == "Papel" || r.Recurso == "Permissao"))
                .CountAsync();

            // Picos de atividade (últimas 24 horas por hora)
            var atividadePorHora = await _context.RegistrosAuditoria
                .Where(r => r.DataHora >= ultimasHoras)
                .GroupBy(r => r.DataHora.Hour)
                .Select(g => new { Hora = g.Key, Total = g.Count() })
                .OrderBy(x => x.Hora)
                .ToListAsync();

            var metricas = new
            {
                ResumoPeriodo = new
                {
                    Ultimas24Horas = operacoesUltimas24h,
                    Ultimos7Dias = operacoesUltimos7dias,
                    UsuariosAtivosUltimoMes = usuariosAtivosUltimoMes,
                    OperacoesCriticasUltimos7Dias = operacoesCriticas
                },
                RecursosMaisAlterados = recursosAlterados.Select(r => new
                {
                    r.Recurso,
                    r.Total,
                    Percentual = recursosAlterados.Sum(x => x.Total) > 0 
                        ? Math.Round((decimal)r.Total / recursosAlterados.Sum(x => x.Total) * 100, 1)
                        : 0
                }),
                AtividadePorHora = atividadePorHora.Select(a => new
                {
                    Hora = $"{a.Hora:D2}:00",
                    Total = a.Total
                }),
                Alertas = await GerarAlertasCompliance(),
                DataAtualizacao = agora
            };

            return Ok(metricas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter métricas de auditoria");
            return StatusCode(500, new RespostaErro { Erro = "Erro interno", Mensagem = "Erro ao processar solicitação" });
        }
    }

    /// <summary>
    /// Obtém timeline de atividades para um usuário específico
    /// </summary>
    /// <param name="usuarioId">ID do usuário</param>
    /// <param name="dataInicio">Data início</param>
    /// <param name="dataFim">Data fim</param>
    /// <returns>Timeline de atividades do usuário</returns>
    [HttpGet("timeline/usuario/{usuarioId:int}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterTimelineUsuario(
        int usuarioId,
        [FromQuery] DateTime? dataInicio = null,
        [FromQuery] DateTime? dataFim = null)
    {
        try
        {
            if (!TemPermissao("Auditoria.Visualizar"))
            {
                return StatusCode(403, new RespostaErro 
                { 
                    Erro = "Acesso negado", 
                    Mensagem = "Acesso negado para visualizar timeline de usuário" 
                });
            }

            var usuario = await _context.Users.FindAsync(usuarioId);
            if (usuario == null)
            {
                return NotFound(new RespostaErro { Erro = "Não encontrado", Mensagem = "Usuário não encontrado" });
            }

            var inicio = dataInicio ?? DateTime.UtcNow.AddDays(-30);
            var fim = dataFim ?? DateTime.UtcNow;

            var atividades = await _context.RegistrosAuditoria
                .Where(r => r.UsuarioId == usuarioId && 
                           r.DataHora >= inicio && r.DataHora <= fim)
                .OrderByDescending(r => r.DataHora)
                .Select(r => new
                {
                    r.Id,
                    r.Acao,
                    r.Recurso,
                    r.RecursoId,
                    r.DataHora,
                    r.EnderecoIp,
                    r.Observacoes,
                    TemAlteracoes = !string.IsNullOrEmpty(r.DadosAntes) || !string.IsNullOrEmpty(r.DadosDepois),
                    CategoriaAcao = ObterCategoriaAcao(r.Acao),
                    Severidade = CalcularSeveridade(r.Acao, r.Recurso)
                })
                .Take(100)
                .ToListAsync();

            var timeline = new
            {
                Usuario = new
                {
                    usuario.Id,
                    usuario.Nome,
                    usuario.Sobrenome,
                    usuario.Email
                },
                Periodo = new
                {
                    Inicio = inicio,
                    Fim = fim
                },
                TotalAtividades = atividades.Count,
                Atividades = atividades.GroupBy(a => a.DataHora.Date)
                    .Select(g => new
                    {
                        Data = g.Key,
                        TotalOperacoes = g.Count(),
                        Operacoes = g.Select(a => new
                        {
                            a.Id,
                            a.Acao,
                            a.Recurso,
                            a.RecursoId,
                            a.DataHora,
                            a.EnderecoIp,
                            a.Observacoes,
                            a.TemAlteracoes,
                            a.CategoriaAcao,
                            a.Severidade
                        }).OrderByDescending(o => o.DataHora)
                    })
                    .OrderByDescending(d => d.Data),
                Resumo = new
                {
                    AcoesMaisFrequentes = atividades
                        .GroupBy(a => a.Acao)
                        .Select(g => new { Acao = g.Key, Total = g.Count() })
                        .OrderByDescending(x => x.Total)
                        .Take(5),
                    RecursosMaisAlterados = atividades
                        .GroupBy(a => a.Recurso)
                        .Select(g => new { Recurso = g.Key, Total = g.Count() })
                        .OrderByDescending(x => x.Total)
                        .Take(5),
                    IpsUtilizados = atividades
                        .Where(a => !string.IsNullOrEmpty(a.EnderecoIp))
                        .GroupBy(a => a.EnderecoIp)
                        .Select(g => new { IP = g.Key, Total = g.Count() })
                        .OrderByDescending(x => x.Total)
                }
            };

            return Ok(timeline);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter timeline do usuário {UsuarioId}", usuarioId);
            return StatusCode(500, new RespostaErro { Erro = "Erro interno", Mensagem = "Erro ao processar solicitação" });
        }
    }

    #endregion

    #region Busca Avançada

    /// <summary>
    /// Realiza busca avançada em registros de auditoria
    /// </summary>
    /// <param name="request">Parâmetros de busca avançada</param>
    /// <returns>Resultados da busca</returns>
    [HttpPost("busca-avancada")]
    [ProducesResponseType(typeof(RespostaPaginada<RegistroAuditoriaDetalhado>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BuscaAvancada([FromBody] BuscaAvancadaAuditoriaRequest request)
    {
        try
        {
            if (!TemPermissao("Auditoria.Visualizar"))
            {
                return StatusCode(403, new RespostaErro 
                { 
                    Erro = "Acesso negado", 
                    Mensagem = "Acesso negado para busca avançada em auditoria" 
                });
            }

            var validator = new BuscaAvancadaAuditoriaValidator();
            var validationResult = await validator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                return BadRequest(new RespostaErro 
                { 
                    Erro = "Dados inválidos", 
                    Mensagem = "Parâmetros de busca inválidos",
                    Detalhes = validationResult.Errors.Select(e => e.ErrorMessage).ToList()
                });
            }

            var query = _context.RegistrosAuditoria
                .Include(r => r.Usuario)
                .AsQueryable();

            // Aplicar filtros básicos se fornecidos
            if (request.Filtros != null)
            {
                query = AplicarFiltros(query, request.Filtros);
            }

            // Aplicar busca textual
            query = AplicarBuscaTextual(query, request);

            // Contagem total
            var totalItens = await query.CountAsync();

            // Aplicar paginação (usar filtros se disponíveis, senão valores padrão)
            var pagina = request.Filtros?.Pagina ?? 1;
            var itensPorPagina = request.Filtros?.ItensPorPagina ?? 20;

            var registros = await query
                .OrderByDescending(r => r.DataHora)
                .Skip((pagina - 1) * itensPorPagina)
                .Take(itensPorPagina)
                .Select(r => new RegistroAuditoriaDetalhado
                {
                    Id = r.Id,
                    Acao = r.Acao,
                    Recurso = r.Recurso,
                    RecursoId = r.RecursoId,
                    Observacoes = r.Observacoes,
                    DataHora = r.DataHora,
                    EnderecoIp = r.EnderecoIp,
                    UserAgent = r.UserAgent,
                    Usuario = r.Usuario != null ? new UsuarioAuditoria
                    {
                        Id = r.Usuario.Id,
                        Nome = r.Usuario.Nome,
                        Sobrenome = r.Usuario.Sobrenome,
                        Email = r.Usuario.Email ?? ""
                    } : null,
                    DadosAntes = request.IncluirDadosAntes ? ProcessarDadosJson(r.DadosAntes) : null,
                    DadosDepois = request.IncluirDadosDepois ? ProcessarDadosJson(r.DadosDepois) : null,
                    CategoriaAcao = ObterCategoriaAcao(r.Acao),
                    Severidade = CalcularSeveridade(r.Acao, r.Recurso),
                    Alteracoes = GerarResumoAlteracoes(r.DadosAntes, r.DadosDepois)
                })
                .ToListAsync();

            var totalPaginas = (int)Math.Ceiling((double)totalItens / itensPorPagina);

            return Ok(new RespostaPaginada<RegistroAuditoriaDetalhado>
            {
                Dados = registros,
                PaginaAtual = pagina,
                ItensPorPagina = itensPorPagina,
                TotalItens = totalItens,
                TotalPaginas = totalPaginas,
                TemProximaPagina = pagina < totalPaginas,
                TemPaginaAnterior = pagina > 1
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro na busca avançada de auditoria");
            return StatusCode(500, new RespostaErro { Erro = "Erro interno", Mensagem = "Erro ao processar busca" });
        }
    }

    #endregion

    #region Relatórios e Exportação

    /// <summary>
    /// Gera relatório de auditoria baseado nos parâmetros fornecidos
    /// </summary>
    /// <param name="request">Parâmetros do relatório</param>
    /// <returns>Relatório de auditoria</returns>
    [HttpPost("relatorio")]
    [ProducesResponseType(typeof(RelatorioAuditoriaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GerarRelatorio([FromBody] RelatorioAuditoriaRequest request)
    {
        try
        {
            if (!TemPermissao("Auditoria.Visualizar"))
            {
                return StatusCode(403, new RespostaErro 
                { 
                    Erro = "Acesso negado", 
                    Mensagem = "Acesso negado para gerar relatórios de auditoria" 
                });
            }

            var validator = new RelatorioAuditoriaValidator();
            var validationResult = await validator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                return BadRequest(new RespostaErro 
                { 
                    Erro = "Dados inválidos", 
                    Mensagem = "Parâmetros do relatório inválidos",
                    Detalhes = validationResult.Errors.Select(e => e.ErrorMessage).ToList()
                });
            }

            var query = _context.RegistrosAuditoria
                .Include(r => r.Usuario)
                .Where(r => r.DataHora >= request.DataInicio && r.DataHora <= request.DataFim);

            // Aplicar filtros específicos se fornecidos
            if (request.Filtros != null)
            {
                query = AplicarFiltros(query, request.Filtros);
            }

            var totalRegistros = await query.CountAsync();

            object dadosRelatorio = request.TipoRelatorio.ToLower() switch
            {
                "atividade-usuario" => await GerarRelatorioAtividadeUsuario(query, request),
                "historico-recurso" => await GerarRelatorioHistoricoRecurso(query, request),
                "timeline" => await GerarRelatorioTimeline(query, request),
                "compliance" => await GerarRelatorioCompliance(query, request),
                _ => await GerarRelatorioGeral(query, request)
            };

            // Gerar estatísticas do período
            var estatisticas = await GerarEstatisticasRelatorio(query, request.DataInicio, request.DataFim);

            // Gerar resumo executivo
            var resumo = await GerarResumoExecutivo(query, request.TipoRelatorio);

            var relatorio = new RelatorioAuditoriaResponse
            {
                TipoRelatorio = request.TipoRelatorio,
                DataGeracao = DateTime.UtcNow,
                Periodo = $"{request.DataInicio:dd/MM/yyyy} - {request.DataFim:dd/MM/yyyy}",
                TotalRegistros = totalRegistros,
                Dados = dadosRelatorio,
                Resumo = resumo,
                Estatisticas = estatisticas
            };

            // Se o formato não for JSON, processar exportação
            if (request.Formato.ToLower() != "json")
            {
                return await ProcessarExportacao(relatorio, request.Formato);
            }

            return Ok(relatorio);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar relatório de auditoria");
            return StatusCode(500, new RespostaErro { Erro = "Erro interno", Mensagem = "Erro ao processar relatório" });
        }
    }

    /// <summary>
    /// Exporta registros de auditoria em formato específico
    /// </summary>
    /// <param name="formato">Formato da exportação (csv, excel)</param>
    /// <param name="filtros">Filtros para a exportação</param>
    /// <returns>Arquivo para download</returns>
    [HttpGet("exportar")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportarRegistros(
        [FromQuery] string formato = "csv",
        [FromQuery] FiltrosAuditoria? filtros = null)
    {
        try
        {
            if (!TemPermissao("Auditoria.Exportar"))
            {
                return Forbid();
            }

            filtros ??= new FiltrosAuditoria();

            // ✅ CORRIGIDO: Consulta simplificada para evitar problemas com JSON
            var query = _context.RegistrosAuditoria
                .Include(r => r.Usuario)
                .AsQueryable();

            query = AplicarFiltros(query, filtros);
            query = AplicarOrdenacao(query, filtros.OrdenarPor, filtros.DirecaoOrdenacao);

            // ✅ LIMITAÇÃO: Máximo 10000 registros para exportação
            var registros = await query
                .Take(10000)
                .Select(r => new
                {
                    r.Id,
                    r.DataHora,
                    Usuario = r.Usuario != null ? r.Usuario.Nome + " " + r.Usuario.Sobrenome : "N/A",
                    Email = r.Usuario != null ? r.Usuario.Email ?? "" : "",
                    r.Acao,
                    r.Recurso,
                    RecursoId = r.RecursoId ?? "",
                    EnderecoIp = r.EnderecoIp ?? "",
                    Observacoes = r.Observacoes ?? "",
                    // ✅ CORRIGIDO: Verificar se tem alterações sem processar JSON
                    TemAlteracoes = (r.DadosAntes != null && r.DadosAntes != "") || 
                                   (r.DadosDepois != null && r.DadosDepois != "")
                })
                .ToListAsync();

            switch (formato.ToLower())
            {
                case "excel":
                    return await ExportarParaExcel(registros);
                case "csv":
                default:
                    return await ExportarParaCsv(registros);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao exportar registros de auditoria");
            return StatusCode(500, new RespostaErro 
            { 
                Erro = "Erro interno", 
                Mensagem = "Erro ao exportar registros de auditoria" 
            });
        }
    }

    /// <summary>
    /// Obtém histórico detalhado de um recurso específico
    /// </summary>
    /// <param name="recurso">Tipo do recurso (Usuario, Papel, Grupo, etc.)</param>
    /// <param name="recursoId">ID específico do recurso</param>
    /// <param name="dataInicio">Data início do período</param>
    /// <param name="dataFim">Data fim do período</param>
    /// <returns>Histórico detalhado do recurso</returns>
    [HttpGet("historico/{recurso}/{recursoId}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterHistoricoRecurso(
        string recurso,
        string recursoId,
        [FromQuery] DateTime? dataInicio = null,
        [FromQuery] DateTime? dataFim = null)
    {
        try
        {
            if (!TemPermissao("Auditoria.Visualizar"))
            {
                return StatusCode(403, new RespostaErro 
                { 
                    Erro = "Acesso negado", 
                    Mensagem = "Acesso negado para visualizar histórico de recursos" 
                });
            }

            var inicio = dataInicio ?? DateTime.UtcNow.AddDays(-90);
            var fim = dataFim ?? DateTime.UtcNow;

            var historico = await _context.RegistrosAuditoria
                .Include(r => r.Usuario)
                .Where(r => r.Recurso == recurso && 
                           r.RecursoId == recursoId &&
                           r.DataHora >= inicio && r.DataHora <= fim)
                .OrderByDescending(r => r.DataHora)
                .Select(r => new
                {
                    r.Id,
                    r.DataHora,
                    r.Acao,
                    Usuario = r.Usuario != null ? new
                    {
                        r.Usuario.Id,
                        r.Usuario.Nome,
                        r.Usuario.Sobrenome,
                        r.Usuario.Email
                    } : null,
                    r.EnderecoIp,
                    r.Observacoes,
                    DadosAntes = ProcessarDadosJson(r.DadosAntes),
                    DadosDepois = ProcessarDadosJson(r.DadosDepois),
                    Alteracoes = GerarResumoAlteracoes(r.DadosAntes, r.DadosDepois),
                    CategoriaAcao = ObterCategoriaAcao(r.Acao),
                    Severidade = CalcularSeveridade(r.Acao, r.Recurso)
                })
                .ToListAsync();

            if (!historico.Any())
            {
                return NotFound(new RespostaErro 
                { 
                    Erro = "Não encontrado", 
                    Mensagem = "Nenhum registro encontrado para este recurso no período especificado" 
                });
            }

            // Estatísticas do histórico
            var estatisticas = new
            {
                TotalOperacoes = historico.Count,
                PrimeiraOperacao = historico.LastOrDefault()?.DataHora,
                UltimaOperacao = historico.FirstOrDefault()?.DataHora,
                UsuariosEnvolvidos = historico
                    .Where(h => h.Usuario != null)
                    .Select(h => h.Usuario)
                    .GroupBy(u => u!.Id)
                    .Select(g => new
                    {
                        g.First()!.Id,
                        g.First()!.Nome,
                        g.First()!.Sobrenome,
                        TotalOperacoes = g.Count()
                    })
                    .OrderByDescending(u => u.TotalOperacoes),
                AcoesPorTipo = historico
                    .GroupBy(h => h.Acao)
                    .ToDictionary(g => g.Key, g => g.Count()),
                OperacoesPorMes = historico
                    .GroupBy(h => new { h.DataHora.Year, h.DataHora.Month })
                    .Select(g => new
                    {
                        Periodo = $"{g.Key.Month:D2}/{g.Key.Year}",
                        Total = g.Count()
                    })
                    .OrderBy(x => x.Periodo)
            };

            var resultado = new
            {
                Recurso = recurso,
                RecursoId = recursoId,
                Periodo = new
                {
                    Inicio = inicio,
                    Fim = fim
                },
                Historico = historico,
                Estatisticas = estatisticas,
                Resumo = new
                {
                    RecursoExiste = await VerificarExistenciaRecurso(recurso, recursoId),
                    UltimaAlteracao = historico.FirstOrDefault()?.DataHora,
                    TotalAlteracoes = historico.Count(h => h.Alteracoes.Any()),
                    OperacoesCriticas = historico.Count(h => h.Severidade == "Alta" || h.Severidade == "Crítica")
                }
            };

            return Ok(resultado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter histórico do recurso {Recurso}:{RecursoId}", recurso, recursoId);
            return StatusCode(500, new RespostaErro { Erro = "Erro interno", Mensagem = "Erro ao processar solicitação" });
        }
    }

    #endregion

    #region Compliance e Alertas

    /// <summary>
    /// Obtém alertas de compliance baseados na auditoria
    /// </summary>
    /// <param name="periodo">Período para análise (dias)</param>
    /// <returns>Lista de alertas de compliance</returns>
    [HttpGet("compliance/alertas")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ObterAlertasCompliance([FromQuery] int periodo = 30)
    {
        try
        {
            if (!TemPermissao("Auditoria.Visualizar"))
            {
                return StatusCode(403, new RespostaErro 
                { 
                    Erro = "Acesso negado", 
                    Mensagem = "Acesso negado para visualizar alertas de compliance" 
                });
            }

            var dataLimite = DateTime.UtcNow.AddDays(-periodo);

            var alertas = new List<object>();

            // Alertas de múltiplos logins do mesmo usuário em IPs diferentes
            var loginsSuspeitos = await _context.RegistrosAuditoria
                .Where(r => r.DataHora >= dataLimite && 
                           r.Acao.Contains("Login") && 
                           r.UsuarioId != null &&
                           !string.IsNullOrEmpty(r.EnderecoIp))
                .GroupBy(r => new { r.UsuarioId, Data = r.DataHora.Date })
                .Where(g => g.Select(x => x.EnderecoIp).Distinct().Count() > 3)
                .Select(g => new
                {
                    UsuarioId = g.Key.UsuarioId,
                    Data = g.Key.Data,
                    IpsDistintos = g.Select(x => x.EnderecoIp).Distinct().Count(),
                    TotalLogins = g.Count()
                })
                .ToListAsync();

            foreach (var login in loginsSuspeitos)
            {
                var usuario = await _context.Users.FindAsync(login.UsuarioId);
                alertas.Add(new
                {
                    Tipo = "Login Suspeito",
                    Severidade = "Alta",
                    Descricao = $"Usuário {usuario?.Nome} fez {login.TotalLogins} logins de {login.IpsDistintos} IPs diferentes em {login.Data:dd/MM/yyyy}",
                    Usuario = usuario?.Nome ?? "Desconhecido",
                    Data = login.Data,
                    Detalhes = new { login.UsuarioId, login.IpsDistintos, login.TotalLogins }
                });
            }

            // Alertas de operações críticas fora do horário comercial
            var operacoesForaHorario = await _context.RegistrosAuditoria
                .Where(r => r.DataHora >= dataLimite &&
                           (r.Acao.Contains("Excluir") || r.Acao.Contains("Desativar")) &&
                           (r.DataHora.Hour < 8 || r.DataHora.Hour > 18))
                .Include(r => r.Usuario)
                .Select(r => new
                {
                    r.Id,
                    r.Acao,
                    r.Recurso,
                    r.DataHora,
                    Usuario = r.Usuario != null ? r.Usuario.Nome : "Sistema"
                })
                .ToListAsync();

            foreach (var operacao in operacoesForaHorario)
            {
                alertas.Add(new
                {
                    Tipo = "Operação Fora do Horário",
                    Severidade = "Média",
                    Descricao = $"Operação crítica '{operacao.Acao}' no recurso '{operacao.Recurso}' realizada fora do horário comercial",
                    Usuario = operacao.Usuario,
                    Data = operacao.DataHora,
                    Detalhes = new { operacao.Id, operacao.Acao, operacao.Recurso }
                });
            }

            // Alertas de tentativas de acesso negado excessivas
            var tentativasNegadas = await _context.RegistrosAuditoria
                .Where(r => r.DataHora >= dataLimite && 
                           r.Acao.Contains("Negado") &&
                           r.UsuarioId != null)
                .GroupBy(r => new { r.UsuarioId, Data = r.DataHora.Date })
                .Where(g => g.Count() > 10)
                .Select(g => new
                {
                    UsuarioId = g.Key.UsuarioId,
                    Data = g.Key.Data,
                    TotalTentativas = g.Count()
                })
                .ToListAsync();

            foreach (var tentativa in tentativasNegadas)
            {
                var usuario = await _context.Users.FindAsync(tentativa.UsuarioId);
                alertas.Add(new
                {
                    Tipo = "Tentativas de Acesso Excessivas",
                    Severidade = "Alta",
                    Descricao = $"Usuário {usuario?.Nome} teve {tentativa.TotalTentativas} tentativas de acesso negado em {tentativa.Data:dd/MM/yyyy}",
                    Usuario = usuario?.Nome ?? "Desconhecido",
                    Data = tentativa.Data,
                    Detalhes = new { tentativa.UsuarioId, tentativa.TotalTentativas }
                });
            }

            // Alertas de alterações em dados sensíveis
            var alteracoesSensiveis = await _context.RegistrosAuditoria
                .Where(r => r.DataHora >= dataLimite &&
                           (r.Recurso == "Usuario" || r.Recurso == "Papel" || r.Recurso == "Permissao") &&
                           r.Acao.Contains("Editar") &&
                           (!string.IsNullOrEmpty(r.DadosAntes) || !string.IsNullOrEmpty(r.DadosDepois)))
                .Include(r => r.Usuario)
                .Take(50)
                .ToListAsync();

            foreach (var alteracao in alteracoesSensiveis)
            {
                alertas.Add(new
                {
                    Tipo = "Alteração de Dados Sensíveis",
                    Severidade = "Média",
                    Descricao = $"Alteração no recurso sensível '{alteracao.Recurso}' (ID: {alteracao.RecursoId})",
                    Usuario = alteracao.Usuario?.Nome ?? "Sistema",
                    Data = alteracao.DataHora,
                    Detalhes = new { alteracao.Id, alteracao.Recurso, alteracao.RecursoId }
                });
            }

            var resultado = new
            {
                PeriodoAnalise = periodo,
                DataAnalise = DateTime.UtcNow,
                TotalAlertas = alertas.Count,
                AlertasPorSeveridade = alertas
                    .GroupBy(a => ((dynamic)a).Severidade)
                    .ToDictionary(g => g.Key, g => g.Count()),
                AlertasPorTipo = alertas
                    .GroupBy(a => ((dynamic)a).Tipo)
                    .ToDictionary(g => g.Key, g => g.Count()),
                Alertas = alertas.OrderByDescending(a => ((dynamic)a).Data).Take(100)
            };

            return Ok(resultado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter alertas de compliance");
            return StatusCode(500, new RespostaErro { Erro = "Erro interno", Mensagem = "Erro ao processar alertas" });
        }
    }

    #endregion

    #region Métodos Auxiliares

    /// <summary>
    /// Verifica se o usuário atual tem a permissão especificada
    /// </summary>
    private bool TemPermissao(string permissao)
    {
        try
        {
            var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(usuarioId))
                return false;

            // Verificar se é SuperAdmin (tem todas as permissões)
            if (User.IsInRole("SuperAdmin"))
                return true;

            // Verificar permissão específica
            return User.HasClaim("permission", permissao);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Aplica filtros à query de registros de auditoria
    /// </summary>
    private IQueryable<RegistroAuditoria> AplicarFiltros(IQueryable<RegistroAuditoria> query, FiltrosAuditoria filtros)
    {
        // Filtro por usuário (ID)
        if (filtros.UsuarioId.HasValue)
            query = query.Where(r => r.UsuarioId == filtros.UsuarioId);

        // Filtro por nome do usuário
        if (!string.IsNullOrEmpty(filtros.NomeUsuario))
            query = query.Where(r => r.Usuario != null && 
                                   (r.Usuario.Nome.Contains(filtros.NomeUsuario) || 
                                    r.Usuario.Sobrenome.Contains(filtros.NomeUsuario)));

        // Filtro por email do usuário
        if (!string.IsNullOrEmpty(filtros.EmailUsuario))
            query = query.Where(r => r.Usuario != null && 
                                   r.Usuario.Email!.Contains(filtros.EmailUsuario));

        // Filtro por ação
        if (!string.IsNullOrEmpty(filtros.Acao))
            query = query.Where(r => r.Acao.Contains(filtros.Acao));

        // Filtro por recurso
        if (!string.IsNullOrEmpty(filtros.Recurso))
            query = query.Where(r => r.Recurso.Contains(filtros.Recurso));

        // Filtro por ID do recurso
        if (!string.IsNullOrEmpty(filtros.RecursoId))
            query = query.Where(r => r.RecursoId == filtros.RecursoId);

        // Filtro por período
        if (filtros.DataInicio.HasValue)
            query = query.Where(r => r.DataHora >= filtros.DataInicio.Value);

        if (filtros.DataFim.HasValue)
            query = query.Where(r => r.DataHora <= filtros.DataFim.Value);

        // Filtro por endereço IP
        if (!string.IsNullOrEmpty(filtros.EnderecoIp))
            query = query.Where(r => r.EnderecoIp != null && r.EnderecoIp.Contains(filtros.EnderecoIp));

        // Filtro por observações
        if (!string.IsNullOrEmpty(filtros.BuscaObservacoes))
            query = query.Where(r => r.Observacoes != null && r.Observacoes.Contains(filtros.BuscaObservacoes));

        // Filtro apenas com alterações
        if (filtros.ApenasComAlteracoes.HasValue && filtros.ApenasComAlteracoes.Value)
            query = query.Where(r => !string.IsNullOrEmpty(r.DadosAntes) || !string.IsNullOrEmpty(r.DadosDepois));

        // Filtro por categorias de ação
        if (filtros.CategoriasAcao != null && filtros.CategoriasAcao.Any())
        {
            var categorias = filtros.CategoriasAcao.ToList();
            query = query.Where(r => categorias.Any(cat => r.Acao.Contains(cat)));
        }

        // Filtro por termo de busca geral
        if (!string.IsNullOrEmpty(filtros.TermoBusca))
        {
            var termo = filtros.TermoBusca.ToLower();
            query = query.Where(r => r.Acao.ToLower().Contains(termo) ||
                                   r.Recurso.ToLower().Contains(termo) ||
                                   (r.Observacoes != null && r.Observacoes.ToLower().Contains(termo)) ||
                                   (r.Usuario != null && 
                                    (r.Usuario.Nome.ToLower().Contains(termo) || 
                                     r.Usuario.Email!.ToLower().Contains(termo))));
        }

        return query;
    }

    /// <summary>
    /// Aplica ordenação à query de registros de auditoria
    /// </summary>
    private IQueryable<RegistroAuditoria> AplicarOrdenacao(IQueryable<RegistroAuditoria> query, string? ordenarPor, string? direcao)
    {
        var crescente = string.IsNullOrEmpty(direcao) || direcao.ToLower() == "asc";

        return (ordenarPor?.ToLower()) switch
        {
            "acao" => crescente ? query.OrderBy(r => r.Acao) : query.OrderByDescending(r => r.Acao),
            "recurso" => crescente ? query.OrderBy(r => r.Recurso) : query.OrderByDescending(r => r.Recurso),
            "usuario" => crescente ? query.OrderBy(r => r.Usuario != null ? r.Usuario.Nome : "") : 
                                    query.OrderByDescending(r => r.Usuario != null ? r.Usuario.Nome : ""),
            "ip" => crescente ? query.OrderBy(r => r.EnderecoIp ?? "") : query.OrderByDescending(r => r.EnderecoIp ?? ""),
            "datahora" or _ => crescente ? query.OrderBy(r => r.DataHora) : query.OrderByDescending(r => r.DataHora)
        };
    }

    /// <summary>
    /// Processa dados JSON para exibição
    /// </summary>
    private static object? ProcessarDadosJson(string? dadosJson)
    {
        if (string.IsNullOrEmpty(dadosJson))
            return null;

        try
        {
            return JsonSerializer.Deserialize<object>(dadosJson);
        }
        catch
        {
            return dadosJson; // Retorna string original se não conseguir fazer parse
        }
    }

    /// <summary>
    /// Obtém a categoria da ação para agrupamento
    /// </summary>
    private static string ObterCategoriaAcao(string acao)
    {
        var acaoLower = acao.ToLower();
        
        if (acaoLower.Contains("criar") || acaoLower.Contains("adicionar") || acaoLower.Contains("inserir"))
            return "Criação";
        
        if (acaoLower.Contains("editar") || acaoLower.Contains("atualizar") || acaoLower.Contains("alterar"))
            return "Modificação";
        
        if (acaoLower.Contains("excluir") || acaoLower.Contains("remover") || acaoLower.Contains("deletar"))
            return "Remoção";
        
        if (acaoLower.Contains("visualizar") || acaoLower.Contains("listar") || acaoLower.Contains("consultar"))
            return "Consulta";
        
        if (acaoLower.Contains("login") || acaoLower.Contains("logout") || acaoLower.Contains("autenticar"))
            return "Autenticação";
        
        return "Outros";
    }

    /// <summary>
    /// Calcula a severidade da operação
    /// </summary>
    private static string CalcularSeveridade(string acao, string recurso)
    {
        var acaoLower = acao.ToLower();
        var recursoLower = recurso.ToLower();

        if (acaoLower.Contains("excluir") || acaoLower.Contains("remover") || acaoLower.Contains("deletar"))
            return "Crítica";

        if ((recursoLower == "usuario" || recursoLower == "papel" || recursoLower == "permissao") &&
            (acaoLower.Contains("criar") || acaoLower.Contains("editar") || acaoLower.Contains("desativar")))
            return "Alta";

        if (acaoLower.Contains("editar") || acaoLower.Contains("atualizar") || acaoLower.Contains("alterar") ||
            acaoLower.Contains("ativar") || acaoLower.Contains("desativar"))
            return "Média";

        if (acaoLower.Contains("visualizar") || acaoLower.Contains("listar") || acaoLower.Contains("consultar"))
            return "Baixa";

        return "Normal";
    }

    /// <summary>
    /// Gera resumo das alterações comparando dados antes e depois
    /// </summary>
    private List<AlteracaoDetalhada> GerarResumoAlteracoes(string? dadosAntes, string? dadosDepois)
    {
        var alteracoes = new List<AlteracaoDetalhada>();

        if (string.IsNullOrEmpty(dadosAntes) && string.IsNullOrEmpty(dadosDepois))
            return alteracoes;

        try
        {
            JsonElement? antesJson = null;
            JsonElement? depoisJson = null;

            if (!string.IsNullOrEmpty(dadosAntes))
                antesJson = JsonSerializer.Deserialize<JsonElement>(dadosAntes);

            if (!string.IsNullOrEmpty(dadosDepois))
                depoisJson = JsonSerializer.Deserialize<JsonElement>(dadosDepois);

            // Se apenas dados "depois" existem, é uma criação
            if (antesJson == null && depoisJson != null)
            {
                AdicionarAlteracoesCriacao(alteracoes, depoisJson.Value);
            }
            // Se apenas dados "antes" existem, é uma remoção
            else if (antesJson != null && depoisJson == null)
            {
                AdicionarAlteracoesRemocao(alteracoes, antesJson.Value);
            }
            // Se ambos existem, comparar propriedades
            else if (antesJson != null && depoisJson != null)
            {
                CompararPropriedades(alteracoes, antesJson.Value, depoisJson.Value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao processar alterações em dados de auditoria");
        }

        return alteracoes;
    }

    /// <summary>
    /// Adiciona alterações para operações de criação
    /// </summary>
    private void AdicionarAlteracoesCriacao(List<AlteracaoDetalhada> alteracoes, JsonElement dados)
    {
        foreach (var propriedade in dados.EnumerateObject())
        {
            alteracoes.Add(new AlteracaoDetalhada
            {
                Campo = propriedade.Name,
                ValorAnterior = null,
                ValorNovo = ObterValorJsonElement(propriedade.Value),
                TipoAlteracao = "Criação"
            });
        }
    }

    /// <summary>
    /// Adiciona alterações para operações de remoção
    /// </summary>
    private void AdicionarAlteracoesRemocao(List<AlteracaoDetalhada> alteracoes, JsonElement dados)
    {
        foreach (var propriedade in dados.EnumerateObject())
        {
            alteracoes.Add(new AlteracaoDetalhada
            {
                Campo = propriedade.Name,
                ValorAnterior = ObterValorJsonElement(propriedade.Value),
                ValorNovo = null,
                TipoAlteracao = "Remoção"
            });
        }
    }

    /// <summary>
    /// Compara propriedades entre dois objetos JSON
    /// </summary>
    private void CompararPropriedades(List<AlteracaoDetalhada> alteracoes, JsonElement antes, JsonElement depois)
    {
        var propriedadesAntes = antes.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
        var propriedadesDepois = depois.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);

        // Verificar propriedades alteradas ou removidas
        foreach (var prop in propriedadesAntes)
        {
            if (propriedadesDepois.TryGetValue(prop.Key, out var valorDepois))
            {
                var valorAntes = ObterValorJsonElement(prop.Value);
                var valorNovo = ObterValorJsonElement(valorDepois);

                if (!object.Equals(valorAntes, valorNovo))
                {
                    alteracoes.Add(new AlteracaoDetalhada
                    {
                        Campo = prop.Key,
                        ValorAnterior = valorAntes,
                        ValorNovo = valorNovo,
                        TipoAlteracao = "Edição"
                    });
                }
            }
            else
            {
                alteracoes.Add(new AlteracaoDetalhada
                {
                    Campo = prop.Key,
                    ValorAnterior = ObterValorJsonElement(prop.Value),
                    ValorNovo = null,
                    TipoAlteracao = "Remoção"
                });
            }
        }

        // Verificar propriedades adicionadas
        foreach (var prop in propriedadesDepois.Where(p => !propriedadesAntes.ContainsKey(p.Key)))
        {
            alteracoes.Add(new AlteracaoDetalhada
            {
                Campo = prop.Key,
                ValorAnterior = null,
                ValorNovo = ObterValorJsonElement(prop.Value),
                TipoAlteracao = "Adição"
            });
        }
    }

    /// <summary>
    /// Obtém valor tipado de um JsonElement
    /// </summary>
    private object? ObterValorJsonElement(JsonElement elemento)
    {
        return elemento.ValueKind switch
        {
            JsonValueKind.String => elemento.GetString(),
            JsonValueKind.Number => elemento.TryGetInt32(out var intVal) ? intVal : elemento.GetDecimal(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => elemento.EnumerateArray().Select(ObterValorJsonElement).ToArray(),
            JsonValueKind.Object => elemento.EnumerateObject().ToDictionary(p => p.Name, p => ObterValorJsonElement(p.Value)),
            _ => elemento.ToString()
        };
    }

    /// <summary>
    /// Gera contexto adicional da operação
    /// </summary>
    private Dictionary<string, object> GerarContextoOperacao(RegistroAuditoria registro)
    {
        var contexto = new Dictionary<string, object>();

        if (!string.IsNullOrEmpty(registro.EnderecoIp))
        {
            contexto["TipoIP"] = DeterminarTipoIP(registro.EnderecoIp);
        }

        if (!string.IsNullOrEmpty(registro.UserAgent))
        {
            contexto["Navegador"] = ExtrairNavegador(registro.UserAgent);
            contexto["SistemaOperacional"] = ExtrairSistemaOperacional(registro.UserAgent);
        }

        contexto["HorarioOperacao"] = registro.DataHora.Hour < 8 || registro.DataHora.Hour > 18 ? "Fora do horário comercial" : "Horário comercial";
        contexto["DiaSemana"] = registro.DataHora.DayOfWeek.ToString();

        return contexto;
    }

    /// <summary>
    /// Calcula tendência de atividade baseada nos registros por dia
    /// </summary>
    private string CalcularTendenciaAtividade(Dictionary<DateTime, int> registrosPorDia)
    {
        if (registrosPorDia.Count < 3)
            return "Insuficiente";

        var valores = registrosPorDia.OrderBy(x => x.Key).Select(x => x.Value).ToList();
        var metadeInicial = valores.Take(valores.Count / 2).Average();
        var metadeFinal = valores.Skip(valores.Count / 2).Average();

        var diferenca = (metadeFinal - metadeInicial) / metadeInicial * 100;

        return diferenca switch
        {
            > 20 => "Crescendo",
            < -20 => "Decrescendo",
            _ => "Estável"
        };
    }

    /// <summary>
    /// Determina o tipo de IP (privado, público, local)
    /// </summary>
    private static string DeterminarTipoIP(string ip)
    {
        if (ip.StartsWith("127.") || ip == "::1")
            return "Local";

        if (ip.StartsWith("192.168.") || ip.StartsWith("10.") || 
            ip.StartsWith("172.16.") || ip.StartsWith("172.17.") ||
            ip.StartsWith("172.18.") || ip.StartsWith("172.19.") ||
            ip.StartsWith("172.20.") || ip.StartsWith("172.21.") ||
            ip.StartsWith("172.22.") || ip.StartsWith("172.23.") ||
            ip.StartsWith("172.24.") || ip.StartsWith("172.25.") ||
            ip.StartsWith("172.26.") || ip.StartsWith("172.27.") ||
            ip.StartsWith("172.28.") || ip.StartsWith("172.29.") ||
            ip.StartsWith("172.30.") || ip.StartsWith("172.31."))
            return "Privado";

        return "Público";
    }

    /// <summary>
    /// Extrai informações do navegador do User-Agent
    /// </summary>
    private static string ExtrairNavegador(string userAgent)
    {
        var ua = userAgent.ToLower();
        
        if (ua.Contains("chrome")) return "Chrome";
        if (ua.Contains("firefox")) return "Firefox";
        if (ua.Contains("safari")) return "Safari";
        if (ua.Contains("edge")) return "Edge";
        if (ua.Contains("opera")) return "Opera";
        
        return "Desconhecido";
    }

    /// <summary>
    /// Extrai informações do sistema operacional do User-Agent
    /// </summary>
    private static string ExtrairSistemaOperacional(string userAgent)
    {
        var ua = userAgent.ToLower();
        
        if (ua.Contains("windows")) return "Windows";
        if (ua.Contains("macintosh") || ua.Contains("mac os")) return "macOS";
        if (ua.Contains("linux")) return "Linux";
        if (ua.Contains("android")) return "Android";
        if (ua.Contains("iphone") || ua.Contains("ipad")) return "iOS";
        
        return "Desconhecido";
    }

    /// <summary>
    /// Aplica busca textual nos registros
    /// </summary>
    private IQueryable<RegistroAuditoria> AplicarBuscaTextual(IQueryable<RegistroAuditoria> query, BuscaAvancadaAuditoriaRequest request)
    {
        if (string.IsNullOrEmpty(request.TextoBusca))
            return query;

        var termo = request.TextoBusca.ToLower();
        var campos = request.CamposBusca ?? new List<string> { "acao", "recurso", "observacoes" };

        var predicates = new List<System.Linq.Expressions.Expression<Func<RegistroAuditoria, bool>>>();

        foreach (var campo in campos.Select(c => c.ToLower()))
        {
            switch (campo)
            {
                case "acao":
                    if (request.BuscaExata)
                        predicates.Add(r => r.Acao.ToLower() == termo);
                    else
                        predicates.Add(r => r.Acao.ToLower().Contains(termo));
                    break;

                case "recurso":
                    if (request.BuscaExata)
                        predicates.Add(r => r.Recurso.ToLower() == termo);
                    else
                        predicates.Add(r => r.Recurso.ToLower().Contains(termo));
                    break;

                case "observacoes":
                    if (request.BuscaExata)
                        predicates.Add(r => r.Observacoes != null && r.Observacoes.ToLower() == termo);
                    else
                        predicates.Add(r => r.Observacoes != null && r.Observacoes.ToLower().Contains(termo));
                    break;

                case "ip":
                    if (request.BuscaExata)
                        predicates.Add(r => r.EnderecoIp != null && r.EnderecoIp.ToLower() == termo);
                    else
                        predicates.Add(r => r.EnderecoIp != null && r.EnderecoIp.ToLower().Contains(termo));
                    break;

                case "useragent":
                    if (request.BuscaExata)
                        predicates.Add(r => r.UserAgent != null && r.UserAgent.ToLower() == termo);
                    else
                        predicates.Add(r => r.UserAgent != null && r.UserAgent.ToLower().Contains(termo));
                    break;

                case "dadosantes":
                    predicates.Add(r => r.DadosAntes != null && r.DadosAntes.ToLower().Contains(termo));
                    break;

                case "dadosdepois":
                    predicates.Add(r => r.DadosDepois != null && r.DadosDepois.ToLower().Contains(termo));
                    break;
            }
        }

        // Combinar predicados com OR
        if (predicates.Any())
        {
            var combined = predicates.Aggregate((prev, next) => 
                System.Linq.Expressions.Expression.Lambda<Func<RegistroAuditoria, bool>>(
                    System.Linq.Expressions.Expression.OrElse(prev.Body, next.Body),
                    prev.Parameters));

            query = query.Where(combined);
        }

        return query;
    }

    #endregion

    #region Métodos de Relatórios

    /// <summary>
    /// Gera relatório de atividade por usuário
    /// </summary>
    private async Task<object> GerarRelatorioAtividadeUsuario(IQueryable<RegistroAuditoria> query, RelatorioAuditoriaRequest request)
    {
        var usuarioId = request.Filtros?.UsuarioId;
        if (!usuarioId.HasValue)
            throw new ArgumentException("ID do usuário é obrigatório para relatório de atividade");

        var usuario = await _context.Users.FindAsync(usuarioId.Value);
        if (usuario == null)
            throw new ArgumentException("Usuário não encontrado");

        var registros = await query
            .Where(r => r.UsuarioId == usuarioId)
            .OrderByDescending(r => r.DataHora)
            .ToListAsync();

        var atividadesPorDia = registros
            .GroupBy(r => r.DataHora.Date)
            .ToDictionary(g => g.Key, g => new
            {
                Total = g.Count(),
                Acoes = g.GroupBy(r => r.Acao).ToDictionary(ag => ag.Key, ag => ag.Count()),
                Recursos = g.GroupBy(r => r.Recurso).ToDictionary(rg => rg.Key, rg => rg.Count())
            });

        var atividadesPorHora = registros
            .GroupBy(r => r.DataHora.Hour)
            .ToDictionary(g => g.Key, g => g.Count());

        var recursosAlterados = registros
            .Where(r => !string.IsNullOrEmpty(r.DadosAntes) || !string.IsNullOrEmpty(r.DadosDepois))
            .GroupBy(r => new { r.Recurso, r.RecursoId })
            .Select(g => new
            {
                g.Key.Recurso,
                g.Key.RecursoId,
                TotalAlteracoes = g.Count(),
                UltimaAlteracao = g.Max(r => r.DataHora),
                TiposAlteracao = g.Select(r => r.Acao).Distinct().ToList()
            })
            .OrderByDescending(x => x.TotalAlteracoes)
            .ToList();

        return new
        {
            Usuario = new
            {
                usuario.Id,
                usuario.Nome,
                usuario.Sobrenome,
                usuario.Email,
                NomeCompleto = $"{usuario.Nome} {usuario.Sobrenome}".Trim()
            },
            Periodo = new
            {
                Inicio = request.DataInicio,
                Fim = request.DataFim,
                TotalDias = (request.DataFim - request.DataInicio).Days + 1
            },
            Resumo = new
            {
                TotalOperacoes = registros.Count,
                OperacoesPorDia = registros.Count > 0 ? (decimal)registros.Count / Math.Max(1, (request.DataFim - request.DataInicio).Days + 1) : 0,
                TotalRecursosAfetados = registros.Select(r => new { r.Recurso, r.RecursoId }).Distinct().Count(),
                TotalAlteracoes = registros.Count(r => !string.IsNullOrEmpty(r.DadosAntes) || !string.IsNullOrEmpty(r.DadosDepois)),
                AcaoMaisFrequente = registros.GroupBy(r => r.Acao).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key ?? "Nenhuma",
                RecursoMaisAlterado = registros.GroupBy(r => r.Recurso).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key ?? "Nenhum"
            },
            AtividadesPorDia = atividadesPorDia.OrderBy(x => x.Key),
            AtividadesPorHora = atividadesPorHora.OrderBy(x => x.Key),
            RecursosAlterados = recursosAlterados.Take(20),
            AcoesMaisFrequentes = registros
                .GroupBy(r => r.Acao)
                .Select(g => new { Acao = g.Key, Total = g.Count() })
                .OrderByDescending(x => x.Total)
                .Take(10),
            IpsUtilizados = registros
                .Where(r => !string.IsNullOrEmpty(r.EnderecoIp))
                .GroupBy(r => r.EnderecoIp!)
                .Select(g => new { IP = g.Key, Total = g.Count(), UltimoUso = g.Max(r => r.DataHora) })
                .OrderByDescending(x => x.Total)
                .Take(10),
            Tendencias = new
            {
                AtividadeGeral = CalcularTendenciaAtividade(atividadesPorDia.ToDictionary(x => x.Key, x => x.Value.Total)),
                HorarioPico = atividadesPorHora.OrderByDescending(x => x.Value).FirstOrDefault().Key,
                DiasMaisAtivos = atividadesPorDia.OrderByDescending(x => x.Value.Total).Take(5).Select(x => new
                {
                    Data = x.Key,
                    Total = x.Value.Total
                })
            }
        };
    }

    /// <summary>
    /// Gera relatório de histórico de recurso
    /// </summary>
    private async Task<object> GerarRelatorioHistoricoRecurso(IQueryable<RegistroAuditoria> query, RelatorioAuditoriaRequest request)
    {
        var recurso = request.Filtros?.Recurso;
        if (string.IsNullOrEmpty(recurso))
            throw new ArgumentException("Tipo de recurso é obrigatório para relatório de histórico");

        var registros = await query
            .Where(r => r.Recurso == recurso)
            .Include(r => r.Usuario)
            .OrderByDescending(r => r.DataHora)
            .ToListAsync();

        var recursosAfetados = registros
            .Where(r => !string.IsNullOrEmpty(r.RecursoId))
            .GroupBy(r => r.RecursoId!)
            .Select(g => new
            {
                RecursoId = g.Key,
                TotalOperacoes = g.Count(),
                PrimeiraOperacao = g.Min(r => r.DataHora),
                UltimaOperacao = g.Max(r => r.DataHora),
                UsuariosEnvolvidos = g.Where(r => r.Usuario != null)
                                    .Select(r => new { r.Usuario!.Id, r.Usuario.Nome, r.Usuario.Email })
                                    .Distinct()
                                    .Count(),
                AcoesRealizadas = g.Select(r => r.Acao).Distinct().Count(),
                TemAlteracoes = g.Any(r => !string.IsNullOrEmpty(r.DadosAntes) || !string.IsNullOrEmpty(r.DadosDepois)),
                UltimaAlteracao = g.Where(r => !string.IsNullOrEmpty(r.DadosAntes) || !string.IsNullOrEmpty(r.DadosDepois))
                                   .Max(r => (DateTime?)r.DataHora)
            })
            .OrderByDescending(x => x.UltimaOperacao)
            .ToList();

        var operacoesPorMes = registros
            .GroupBy(r => new { r.DataHora.Year, r.DataHora.Month })
            .Select(g => new
            {
                Periodo = $"{g.Key.Month:D2}/{g.Key.Year}",
                Total = g.Count(),
                Alteracoes = g.Count(r => !string.IsNullOrEmpty(r.DadosAntes) || !string.IsNullOrEmpty(r.DadosDepois)),
                UsuariosAtivos = g.Where(r => r.Usuario != null).Select(r => r.UsuarioId).Distinct().Count()
            })
            .OrderBy(x => x.Periodo)
            .ToList();

        return new
        {
            Recurso = recurso,
            Periodo = new
            {
                Inicio = request.DataInicio,
                Fim = request.DataFim
            },
            Resumo = new
            {
                TotalOperacoes = registros.Count,
                TotalRecursosAfetados = recursosAfetados.Count,
                TotalUsuariosEnvolvidos = registros.Where(r => r.Usuario != null).Select(r => r.UsuarioId).Distinct().Count(),
                TotalAlteracoes = registros.Count(r => !string.IsNullOrEmpty(r.DadosAntes) || !string.IsNullOrEmpty(r.DadosDepois)),
                RecursoMaisAlterado = recursosAfetados.OrderByDescending(x => x.TotalOperacoes).FirstOrDefault()?.RecursoId,
                UltimaAtividade = registros.FirstOrDefault()?.DataHora
            },
            RecursosAfetados = recursosAfetados.Take(50),
            OperacoesPorMes = operacoesPorMes,
            AcoesPorTipo = registros
                .GroupBy(r => r.Acao)
                .Select(g => new { Acao = g.Key, Total = g.Count() })
                .OrderByDescending(x => x.Total),
            UsuariosMaisAtivos = registros
                .Where(r => r.Usuario != null)
                .GroupBy(r => new { r.Usuario!.Id, r.Usuario.Nome, r.Usuario.Email })
                .Select(g => new
                {
                    g.Key.Id,
                    g.Key.Nome,
                    g.Key.Email,
                    TotalOperacoes = g.Count(),
                    UltimaOperacao = g.Max(r => r.DataHora)
                })
                .OrderByDescending(x => x.TotalOperacoes)
                .Take(10)
        };
    }

    /// <summary>
    /// Gera relatório de timeline
    /// </summary>
    private async Task<object> GerarRelatorioTimeline(IQueryable<RegistroAuditoria> query, RelatorioAuditoriaRequest request)
    {
        var registros = await query
            .Include(r => r.Usuario)
            .OrderByDescending(r => r.DataHora)
            .Take(1000) // Limitar timeline a 1000 eventos
            .ToListAsync();

        var timelineEventos = registros
            .GroupBy(r => r.DataHora.Date)
            .Select(g => new
            {
                Data = g.Key,
                TotalEventos = g.Count(),
                EventosPorHora = g.GroupBy(r => r.DataHora.Hour)
                                 .ToDictionary(h => h.Key, h => h.Count()),
                Eventos = g.Select(r => new
                {
                    r.Id,
                    r.DataHora,
                    r.Acao,
                    r.Recurso,
                    r.RecursoId,
                    Usuario = r.Usuario != null ? $"{r.Usuario.Nome} {r.Usuario.Sobrenome}".Trim() : "Sistema",
                    r.EnderecoIp,
                    CategoriaAcao = ObterCategoriaAcao(r.Acao),
                    Severidade = CalcularSeveridade(r.Acao, r.Recurso),
                    TemAlteracoes = !string.IsNullOrEmpty(r.DadosAntes) || !string.IsNullOrEmpty(r.DadosDepois)
                }).OrderByDescending(e => e.DataHora).Take(50) // Máximo 50 eventos por dia
            })
            .OrderByDescending(x => x.Data)
            .ToList();

        return new
        {
            Periodo = new
            {
                Inicio = request.DataInicio,
                Fim = request.DataFim
            },
            Resumo = new
            {
                TotalEventos = registros.Count,
                EventosLimitados = registros.Count >= 1000,
                DiasMaisAtivos = timelineEventos.OrderByDescending(x => x.TotalEventos).Take(5),
                CategoriasMaisFrequentes = registros
                    .GroupBy(r => ObterCategoriaAcao(r.Acao))
                    .Select(g => new { Categoria = g.Key, Total = g.Count() })
                    .OrderByDescending(x => x.Total)
                    .Take(5)
            },
            Timeline = timelineEventos,
            Estatisticas = new
            {
                EventosPorSeveridade = registros
                    .GroupBy(r => CalcularSeveridade(r.Acao, r.Recurso))
                    .ToDictionary(g => g.Key, g => g.Count()),
                RecursosMaisAfetados = registros
                    .GroupBy(r => r.Recurso)
                    .Select(g => new { Recurso = g.Key, Total = g.Count() })
                    .OrderByDescending(x => x.Total)
                    .Take(10),
                UsuariosMaisAtivos = registros
                    .Where(r => r.Usuario != null)
                    .GroupBy(r => new { r.Usuario!.Id, r.Usuario.Nome })
                    .Select(g => new { g.Key.Nome, Total = g.Count() })
                    .OrderByDescending(x => x.Total)
                    .Take(10)
            }
        };
    }

    /// <summary>
    /// Gera relatório de compliance
    /// </summary>
    private async Task<object> GerarRelatorioCompliance(IQueryable<RegistroAuditoria> query, RelatorioAuditoriaRequest request)
    {
        var registros = await query
            .Include(r => r.Usuario)
            .ToListAsync();

        // Análise de operações críticas
        var operacoesCriticas = registros
            .Where(r => CalcularSeveridade(r.Acao, r.Recurso) == "Crítica")
            .GroupBy(r => new { r.DataHora.Date, r.Usuario?.Nome, r.Acao })
            .Select(g => new
            {
                Data = g.Key.Date,
                Usuario = g.Key.Nome ?? "Sistema",
                Acao = g.Key.Acao,
                Frequencia = g.Count(),
                Registros = g.Select(r => new { r.Id, r.DataHora, r.Recurso, r.RecursoId }).ToList()
            })
            .OrderByDescending(x => x.Frequencia)
            .ToList();

        // Análise de acessos fora do horário
        var acessosForaHorario = registros
            .Where(r => r.DataHora.Hour < 8 || r.DataHora.Hour > 18 || 
                       r.DataHora.DayOfWeek == DayOfWeek.Saturday || 
                       r.DataHora.DayOfWeek == DayOfWeek.Sunday)
            .GroupBy(r => new { r.DataHora.Date, r.Usuario?.Nome })
            .Select(g => new
            {
                Data = g.Key.Date,
                Usuario = g.Key.Nome ?? "Sistema",
                TotalOperacoes = g.Count(),
                OperacoesCriticas = g.Count(r => CalcularSeveridade(r.Acao, r.Recurso) == "Crítica"),
                HorariosAcesso = g.Select(r => r.DataHora.Hour).Distinct().OrderBy(h => h).ToList()
            })
            .Where(x => x.TotalOperacoes > 5) // Filtrar apenas atividade significativa
            .OrderByDescending(x => x.OperacoesCriticas)
            .ToList();

        // Análise de múltiplos IPs
        var usuariosMultiplosIps = registros
            .Where(r => r.Usuario != null && !string.IsNullOrEmpty(r.EnderecoIp))
            .GroupBy(r => new { r.Usuario!.Id, r.Usuario.Nome, Data = r.DataHora.Date })
            .Where(g => g.Select(r => r.EnderecoIp).Distinct().Count() > 2)
            .Select(g => new
            {
                g.Key.Id,
                g.Key.Nome,
                g.Key.Data,
                TotalIPs = g.Select(r => r.EnderecoIp).Distinct().Count(),
                IPs = g.Select(r => r.EnderecoIp).Distinct().ToList(),
                TotalOperacoes = g.Count()
            })
            .OrderByDescending(x => x.TotalIPs)
            .ToList();

        // Análise de alterações em dados sensíveis
        var alteracoesSensiveis = registros
            .Where(r => (r.Recurso == "Usuario" || r.Recurso == "Papel" || r.Recurso == "Permissao") &&
                       (!string.IsNullOrEmpty(r.DadosAntes) || !string.IsNullOrEmpty(r.DadosDepois)))
            .GroupBy(r => new { r.Recurso, r.Usuario?.Nome })
            .Select(g => new
            {
                Recurso = g.Key.Recurso,
                Usuario = g.Key.Nome ?? "Sistema",
                TotalAlteracoes = g.Count(),
                RecursosAfetados = g.Select(r => r.RecursoId).Distinct().Count(),
                UltimaAlteracao = g.Max(r => r.DataHora)
            })
            .OrderByDescending(x => x.TotalAlteracoes)
            .ToList();

        return new
        {
            Periodo = new
            {
                Inicio = request.DataInicio,
                Fim = request.DataFim
            },
            ResumoCompliance = new
            {
                TotalRegistros = registros.Count,
                OperacoesCriticas = operacoesCriticas.Sum(x => x.Frequencia),
                AcessosForaHorario = acessosForaHorario.Sum(x => x.TotalOperacoes),
                UsuariosComMultiplosIPs = usuariosMultiplosIps.Count,
                AlteracoesSensiveis = alteracoesSensiveis.Sum(x => x.TotalAlteracoes),
                NivelRisco = CalcularNivelRisco(operacoesCriticas.Count, acessosForaHorario.Count, usuariosMultiplosIps.Count)
            },
            OperacoesCriticas = operacoesCriticas.Take(20),
            AcessosForaHorario = acessosForaHorario.Take(20),
            UsuariosMultiplosIPs = usuariosMultiplosIps.Take(20),
            AlteracoesSensiveis = alteracoesSensiveis.Take(20),
            Recomendacoes = GerarRecomendacoesCompliance(operacoesCriticas, acessosForaHorario, usuariosMultiplosIps, alteracoesSensiveis),
            Alertas = await GerarAlertasCompliance()
        };
    }

    /// <summary>
    /// Gera relatório geral
    /// </summary>
    private async Task<object> GerarRelatorioGeral(IQueryable<RegistroAuditoria> query, RelatorioAuditoriaRequest request)
    {
        var registros = await query
            .Include(r => r.Usuario)
            .ToListAsync();

        return new
        {
            Periodo = new
            {
                Inicio = request.DataInicio,
                Fim = request.DataFim
            },
            Resumo = new
            {
                TotalRegistros = registros.Count,
                TotalUsuarios = registros.Where(r => r.Usuario != null).Select(r => r.UsuarioId).Distinct().Count(),
                TotalRecursos = registros.Select(r => r.Recurso).Distinct().Count(),
                TotalAlteracoes = registros.Count(r => !string.IsNullOrEmpty(r.DadosAntes) || !string.IsNullOrEmpty(r.DadosDepois))
            },
            Distribuicoes = new
            {
                PorAcao = registros.GroupBy(r => r.Acao).ToDictionary(g => g.Key, g => g.Count()),
                PorRecurso = registros.GroupBy(r => r.Recurso).ToDictionary(g => g.Key, g => g.Count()),
                PorUsuario = registros.Where(r => r.Usuario != null)
                                    .GroupBy(r => r.Usuario!.Nome)
                                    .ToDictionary(g => g.Key, g => g.Count()),
                PorHora = registros.GroupBy(r => r.DataHora.Hour).ToDictionary(g => g.Key, g => g.Count())
            },
            Tendencias = new
            {
                AtividadePorDia = registros.GroupBy(r => r.DataHora.Date).ToDictionary(g => g.Key, g => g.Count()),
                CrescimentoSemanal = CalcularCrescimentoSemanal(registros)
            }
        };
    }

    /// <summary>
    /// Gera estatísticas para o relatório
    /// </summary>
    private async Task<EstatisticasAuditoria> GerarEstatisticasRelatorio(IQueryable<RegistroAuditoria> query, DateTime inicio, DateTime fim)
    {
        var registros = await query.Include(r => r.Usuario).ToListAsync();

        var registrosPorDia = registros
            .GroupBy(r => r.DataHora.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        var usuariosMaisAtivos = registros
            .Where(r => r.Usuario != null)
            .GroupBy(r => new { r.Usuario!.Id, r.Usuario.Nome, r.Usuario.Email })
            .Select(g => new UsuarioAtivo
            {
                UsuarioId = g.Key.Id,
                Nome = g.Key.Nome,
                Email = g.Key.Email ?? "",
                TotalOperacoes = g.Count(),
                UltimaOperacao = g.Max(r => r.DataHora),
                OperacoesPorTipo = g.GroupBy(r => r.Acao).ToDictionary(og => og.Key, og => og.Count())
            })
            .OrderByDescending(x => x.TotalOperacoes)
            .Take(10)
            .ToList();

        return new EstatisticasAuditoria
        {
            TotalRegistros = registros.Count,
            RegistrosPorDia = registrosPorDia,
            AcoesMaisFrequentes = registros.GroupBy(r => r.Acao).ToDictionary(g => g.Key, g => g.Count()),
            RecursosMaisAcessados = registros.GroupBy(r => r.Recurso).ToDictionary(g => g.Key, g => g.Count()),
            UsuariosMaisAtivos = usuariosMaisAtivos,
            AtividadePorHora = registros.GroupBy(r => r.DataHora.Hour).ToDictionary(g => g.Key, g => g.Count()),
            IpsMaisFrequentes = registros.Where(r => !string.IsNullOrEmpty(r.EnderecoIp))
                                       .GroupBy(r => r.EnderecoIp!)
                                       .ToDictionary(g => g.Key, g => g.Count()),
            DataInicio = inicio,
            DataFim = fim,
            MediaOperacoesPorDia = registros.Count > 0 ? (decimal)registros.Count / Math.Max(1, (fim - inicio).Days + 1) : 0,
            TendenciaAtividade = CalcularTendenciaAtividade(registrosPorDia)
        };
    }

    /// <summary>
    /// Gera resumo executivo do relatório
    /// </summary>
    private async Task<ResumoExecutivo> GerarResumoExecutivo(IQueryable<RegistroAuditoria> query, string tipoRelatorio)
    {
        var registros = await query.Include(r => r.Usuario).ToListAsync();

        var alertasCompliance = new List<string>();
        var recomendacoesSecurity = new List<string>();

        // Verificar operações críticas
        var operacoesCriticas = registros.Count(r => CalcularSeveridade(r.Acao, r.Recurso) == "Crítica");
        if (operacoesCriticas > 10)
        {
            alertasCompliance.Add($"Alto número de operações críticas detectadas: {operacoesCriticas}");
            recomendacoesSecurity.Add("Revisar políticas de acesso para operações críticas");
        }

        // Verificar acessos fora do horário
        var acessosForaHorario = registros.Count(r => r.DataHora.Hour < 8 || r.DataHora.Hour > 18);
        if (acessosForaHorario > registros.Count * 0.3)
        {
            alertasCompliance.Add("Percentual elevado de acessos fora do horário comercial");
            recomendacoesSecurity.Add("Implementar controles adicionais para acesso fora do horário");
        }

        // Verificar usuários com múltiplos IPs
        var usuariosMultiplosIps = registros
            .Where(r => r.Usuario != null && !string.IsNullOrEmpty(r.EnderecoIp))
            .GroupBy(r => r.Usuario!.Id)
            .Count(g => g.Select(r => r.EnderecoIp).Distinct().Count() > 3);

        if (usuariosMultiplosIps > 0)
        {
            alertasCompliance.Add($"{usuariosMultiplosIps} usuários com múltiplos IPs detectados");
            recomendacoesSecurity.Add("Monitorar padrões de acesso de usuários com múltiplos IPs");
        }

        return new ResumoExecutivo
        {
            TotalUsuariosAtivos = registros.Where(r => r.Usuario != null).Select(r => r.UsuarioId).Distinct().Count(),
            TotalRecursosAfetados = registros.Select(r => new { r.Recurso, r.RecursoId }).Distinct().Count(),
            AcaoMaisFrequente = registros.GroupBy(r => r.Acao).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key ?? "Nenhuma",
            RecursoMaisAlterado = registros.GroupBy(r => r.Recurso).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key ?? "Nenhum",
            HorarioPicoAtividade = $"{registros.GroupBy(r => r.DataHora.Hour).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key ?? 0}:00",
            AlertasCompliance = alertasCompliance,
            RecomendacoesSecurity = recomendacoesSecurity
        };
    }

    #endregion

    #region Métodos de Exportação

    /// <summary>
    /// Processa exportação do relatório em formato específico
    /// </summary>
    private async Task<IActionResult> ProcessarExportacao(RelatorioAuditoriaResponse relatorio, string formato)
    {
        try
        {
            return formato.ToLower() switch
            {
                "csv" => await ExportarRelatorioParaCsv(relatorio),
                "excel" => await ExportarRelatorioParaExcel(relatorio),
                "pdf" => await ExportarRelatorioParaPdf(relatorio),
                _ => BadRequest(new RespostaErro { Erro = "Formato inválido", Mensagem = "Formato de exportação não suportado" })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar exportação em formato {Formato}", formato);
            return StatusCode(500, new RespostaErro { Erro = "Erro interno", Mensagem = "Erro ao processar exportação" });
        }
    }

    /// <summary>
    /// Exporta registros para formato CSV
    /// </summary>
    private async Task<IActionResult> ExportarParaCsv(dynamic registros)
    {
        try
        {
            return await Task.Run(() =>
            {
                var csv = new StringBuilder();
                
                // Cabeçalho
                csv.AppendLine("Id,DataHora,Usuario,Email,Acao,Recurso,RecursoId,EnderecoIP,Observacoes,TemAlteracoes");

                // Dados
                foreach (var registro in registros)
                {
                    csv.AppendLine($"{registro.Id}," +
                                  $"{registro.DataHora:yyyy-MM-dd HH:mm:ss}," +
                                  $"\"{EscaparCsv(registro.Usuario)}\"," +
                                  $"\"{EscaparCsv(registro.Email)}\"," +
                                  $"\"{EscaparCsv(registro.Acao)}\"," +
                                  $"\"{EscaparCsv(registro.Recurso)}\"," +
                                  $"\"{EscaparCsv(registro.RecursoId)}\"," +
                                  $"\"{EscaparCsv(registro.EnderecoIp)}\"," +
                                  $"\"{EscaparCsv(registro.Observacoes)}\"," +
                                  $"{registro.TemAlteracoes}");
                }

                var bytes = Encoding.UTF8.GetBytes(csv.ToString());
                var nomeArquivo = $"auditoria_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                return File(bytes, "text/csv", nomeArquivo);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao exportar para CSV");
            throw;
        }
    }

    /// <summary>
    /// Exporta relatório para formato CSV
    /// </summary>
    private async Task<IActionResult> ExportarRelatorioParaCsv(RelatorioAuditoriaResponse relatorio)
    {
        try
        {
            var result = await Task.Run(() =>
            {
                var csv = new StringBuilder();
                
                // Cabeçalho do relatório
                csv.AppendLine($"# Relatório de Auditoria - {relatorio.TipoRelatorio}");
                csv.AppendLine($"# Período: {relatorio.Periodo}");
                csv.AppendLine($"# Gerado em: {relatorio.DataGeracao:dd/MM/yyyy HH:mm:ss}");
                csv.AppendLine($"# Total de Registros: {relatorio.TotalRegistros}");
                csv.AppendLine();

                // Resumo Executivo
                csv.AppendLine("## Resumo Executivo");
                csv.AppendLine($"Usuários Ativos,{relatorio.Resumo.TotalUsuariosAtivos}");
                csv.AppendLine($"Recursos Afetados,{relatorio.Resumo.TotalRecursosAfetados}");
                csv.AppendLine($"Ação Mais Frequente,\"{EscaparCsv(relatorio.Resumo.AcaoMaisFrequente)}\"");
                csv.AppendLine($"Recurso Mais Alterado,\"{EscaparCsv(relatorio.Resumo.RecursoMaisAlterado)}\"");
                csv.AppendLine($"Horário Pico,\"{EscaparCsv(relatorio.Resumo.HorarioPicoAtividade)}\"");
                csv.AppendLine();

                // Alertas de Compliance
                if (relatorio.Resumo.AlertasCompliance.Any())
                {
                    csv.AppendLine("## Alertas de Compliance");
                    foreach (var alerta in relatorio.Resumo.AlertasCompliance)
                    {
                        csv.AppendLine($"\"{EscaparCsv(alerta)}\"");
                    }
                    csv.AppendLine();
                }

                // Estatísticas básicas
                csv.AppendLine("## Estatísticas");
                csv.AppendLine($"Total de Registros,{relatorio.Estatisticas.TotalRegistros}");
                csv.AppendLine($"Média Operações/Dia,{relatorio.Estatisticas.MediaOperacoesPorDia:F2}");
                csv.AppendLine($"Tendência de Atividade,\"{EscaparCsv(relatorio.Estatisticas.TendenciaAtividade)}\"");

                var bytes = Encoding.UTF8.GetBytes(csv.ToString());
                var nomeArquivo = $"relatorio_auditoria_{relatorio.TipoRelatorio}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                return File(bytes, "text/csv", nomeArquivo);
            });

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao exportar relatório para CSV");
            throw;
        }
    }

    /// <summary>
    /// Exporta registros para formato Excel
    /// </summary>
    private async Task<IActionResult> ExportarParaExcel(dynamic registros)
    {
        try
        {
            return await Task.Run(() =>
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Auditoria");

                // Cabeçalhos
                var cabecalhos = new[] { "ID", "Data/Hora", "Usuário", "Email", "Ação", "Recurso", "Recurso ID", "IP", "Observações", "Tem Alterações" };
                for (int i = 0; i < cabecalhos.Length; i++)
                {
                    worksheet.Cell(1, i + 1).Value = cabecalhos[i];
                    worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                    worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                }

                // Dados
                int linha = 2;
                foreach (var registro in registros)
                {
                    worksheet.Cell(linha, 1).Value = registro.Id;
                    worksheet.Cell(linha, 2).Value = registro.DataHora;
                    worksheet.Cell(linha, 3).Value = registro.Usuario;
                    worksheet.Cell(linha, 4).Value = registro.Email;
                    worksheet.Cell(linha, 5).Value = registro.Acao;
                    worksheet.Cell(linha, 6).Value = registro.Recurso;
                    worksheet.Cell(linha, 7).Value = registro.RecursoId;
                    worksheet.Cell(linha, 8).Value = registro.EnderecoIp;
                    worksheet.Cell(linha, 9).Value = registro.Observacoes;
                    worksheet.Cell(linha, 10).Value = registro.TemAlteracoes ? "Sim" : "Não";
                    linha++;
                }

                // Auto-ajustar colunas
                worksheet.ColumnsUsed().AdjustToContents();

                // Aplicar filtros
                worksheet.RangeUsed().SetAutoFilter();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;

                var nomeArquivo = $"auditoria_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nomeArquivo);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao exportar para Excel");
            throw;
        }
    }

    /// <summary>
    /// Exporta relatório para formato Excel
    /// </summary>
    private async Task<IActionResult> ExportarRelatorioParaExcel(RelatorioAuditoriaResponse relatorio)
    {
        try
        {
            return await Task.Run(() =>
            {
                using var workbook = new XLWorkbook();

                // Planilha de Resumo
                var worksheetResumo = workbook.Worksheets.Add("Resumo");
                CriarPlanilhaResumo(worksheetResumo, relatorio);

                // Planilha de Estatísticas
                var worksheetEstatisticas = workbook.Worksheets.Add("Estatísticas");
                CriarPlanilhaEstatisticas(worksheetEstatisticas, relatorio.Estatisticas);

                // Planilha de Dados (se disponível)
                if (relatorio.Dados != null)
                {
                    var worksheetDados = workbook.Worksheets.Add("Dados");
                    CriarPlanilhaDados(worksheetDados, relatorio.Dados, relatorio.TipoRelatorio);
                }

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;

                var nomeArquivo = $"relatorio_auditoria_{relatorio.TipoRelatorio}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nomeArquivo);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao exportar relatório para Excel");
            throw;
        }
    }

    /// <summary>
    /// Exporta relatório para formato PDF
    /// </summary>
    private async Task<IActionResult> ExportarRelatorioParaPdf(RelatorioAuditoriaResponse relatorio)
    {
        try
        {
            // Nota: Implementação básica - em produção usar bibliotecas como iTextSharp ou DinkToPdf
            var result = await Task.Run(() =>
            {
                var html = GerarHtmlRelatorio(relatorio);

                // Converter HTML para PDF (implementação simplificada)
                var bytes = Encoding.UTF8.GetBytes(html);
                var nomeArquivo = $"relatorio_auditoria_{relatorio.TipoRelatorio}_{DateTime.Now:yyyyMMdd_HHmmss}.html";

                return File(bytes, "text/html", nomeArquivo);
            });

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao exportar relatório para PDF");
            throw;
        }
    }

    /// <summary>
    /// Escapa caracteres especiais para CSV
    /// </summary>
    private string EscaparCsv(string? valor)
    {
        if (string.IsNullOrEmpty(valor))
            return string.Empty;

        return valor.Replace("\"", "\"\"").Replace("\n", " ").Replace("\r", " ");
    }

    #endregion

    #region Métodos de Compliance e Alertas

    /// <summary>
    /// Gera alertas de compliance
    /// </summary>
    private async Task<List<string>> GerarAlertasCompliance()
    {
        var alertas = new List<string>();
        var dataLimite = DateTime.UtcNow.AddDays(-7);

        try
        {
            // Alerta: Muitas operações críticas
            var operacoesCriticas = await _context.RegistrosAuditoria
                .Where(r => r.DataHora >= dataLimite)
                .CountAsync(r => r.Acao.Contains("Excluir") || r.Acao.Contains("Remover"));

            if (operacoesCriticas > 50)
            {
                alertas.Add($"Alto número de operações críticas nos últimos 7 dias: {operacoesCriticas}");
            }

            // Alerta: Acessos fora do horário
            var acessosForaHorario = await _context.RegistrosAuditoria
                .Where(r => r.DataHora >= dataLimite && (r.DataHora.Hour < 7 || r.DataHora.Hour > 19))
                .CountAsync();

            if (acessosForaHorario > 100)
            {
                alertas.Add($"Muitos acessos fora do horário comercial: {acessosForaHorario}");
            }

            // Alerta: Falhas de autenticação
            var falhasAuth = await _context.RegistrosAuditoria
                .Where(r => r.DataHora >= dataLimite && r.Acao.Contains("Falha"))
                .CountAsync();

            if (falhasAuth > 20)
            {
                alertas.Add($"Múltiplas falhas de autenticação detectadas: {falhasAuth}");
            }

            // Alerta: Usuários com múltiplos IPs
            var usuariosMultiplosIps = await _context.RegistrosAuditoria
                .Where(r => r.DataHora >= dataLimite && r.UsuarioId != null && !string.IsNullOrEmpty(r.EnderecoIp))
                .GroupBy(r => r.UsuarioId)
                .Where(g => g.Select(x => x.EnderecoIp).Distinct().Count() > 5)
                .CountAsync();

            if (usuariosMultiplosIps > 0)
            {
                alertas.Add($"Usuários com múltiplos IPs detectados: {usuariosMultiplosIps}");
            }

            return alertas;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar alertas de compliance");
            return new List<string> { "Erro ao verificar alertas de compliance" };
        }
    }

    /// <summary>
    /// Verifica se um recurso ainda existe no sistema
    /// </summary>
    private async Task<bool> VerificarExistenciaRecurso(string recurso, string recursoId)
    {
        try
        {
            return recurso.ToLower() switch
            {
                "usuario" => await _context.Users.AnyAsync(u => u.Id.ToString() == recursoId),
                "papel" => await _context.Papeis.AnyAsync(p => p.Id.ToString() == recursoId),
                "grupo" => await _context.Grupos.AnyAsync(g => g.Id.ToString() == recursoId),
                "permissao" => await _context.Permissoes.AnyAsync(p => p.Id.ToString() == recursoId),
                _ => false
            };
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Calcula nível de risco baseado em métricas de compliance
    /// </summary>
    private string CalcularNivelRisco(int operacoesCriticas, int acessosForaHorario, int usuariosMultiplosIps)
    {
        var pontuacao = 0;

        if (operacoesCriticas > 20) pontuacao += 3;
        else if (operacoesCriticas > 10) pontuacao += 2;
        else if (operacoesCriticas > 5) pontuacao += 1;

        if (acessosForaHorario > 50) pontuacao += 3;
        else if (acessosForaHorario > 20) pontuacao += 2;
        else if (acessosForaHorario > 10) pontuacao += 1;

        if (usuariosMultiplosIps > 5) pontuacao += 3;
        else if (usuariosMultiplosIps > 2) pontuacao += 2;
        else if (usuariosMultiplosIps > 0) pontuacao += 1;

        return pontuacao switch
        {
            >= 7 => "Crítico",
            >= 5 => "Alto",
            >= 3 => "Médio",
            >= 1 => "Baixo",
            _ => "Muito Baixo"
        };
    }

    /// <summary>
    /// Gera recomendações de compliance
    /// </summary>
    private List<string> GerarRecomendacoesCompliance(
        dynamic operacoesCriticas,
        dynamic acessosForaHorario,
        dynamic usuariosMultiplosIps,
        dynamic alteracoesSensiveis)
    {
        var recomendacoes = new List<string>();

        if (operacoesCriticas.Count > 10)
        {
            recomendacoes.Add("Implementar aprovação obrigatória para operações críticas");
            recomendacoes.Add("Configurar alertas em tempo real para exclusões");
        }

        if (acessosForaHorario.Count > 20)
        {
            recomendacoes.Add("Restringir acesso fora do horário comercial");
            recomendacoes.Add("Implementar autenticação adicional para acessos noturnos");
        }

        if (usuariosMultiplosIps.Count > 5)
        {
            recomendacoes.Add("Implementar controle de localização geográfica");
            recomendacoes.Add("Configurar alertas para múltiplos IPs por usuário");
        }

        if (alteracoesSensiveis.Count > 50)
        {
            recomendacoes.Add("Implementar dupla verificação para dados sensíveis");
            recomendacoes.Add("Criar políticas de retenção de dados");
        }

        if (!recomendacoes.Any())
        {
            recomendacoes.Add("Sistema operando dentro dos parâmetros de segurança");
        }

        return recomendacoes;
    }

    /// <summary>
    /// Calcula crescimento semanal de atividade
    /// </summary>
    private object CalcularCrescimentoSemanal(List<RegistroAuditoria> registros)
    {
        var agora = DateTime.UtcNow;
        var semanaAtual = registros.Count(r => r.DataHora >= agora.AddDays(-7));
        var semanaAnterior = registros.Count(r => r.DataHora >= agora.AddDays(-14) && r.DataHora < agora.AddDays(-7));

        var crescimento = semanaAnterior > 0 ? ((decimal)(semanaAtual - semanaAnterior) / semanaAnterior * 100) : 0;

        return new
        {
            SemanaAtual = semanaAtual,
            SemanaAnterior = semanaAnterior,
            Crescimento = Math.Round(crescimento, 2),
            Tendencia = crescimento > 10 ? "Crescendo" : crescimento < -10 ? "Decrescendo" : "Estável"
        };
    }

    #endregion

    #region Métodos Auxiliares de Exportação

    /// <summary>
    /// Cria planilha de resumo no Excel
    /// </summary>
    private void CriarPlanilhaResumo(IXLWorksheet worksheet, RelatorioAuditoriaResponse relatorio)
    {
        worksheet.Cell(1, 1).Value = "Relatório de Auditoria";
        worksheet.Cell(1, 1).Style.Font.FontSize = 16;
        worksheet.Cell(1, 1).Style.Font.Bold = true;

        worksheet.Cell(2, 1).Value = "Tipo:";
        worksheet.Cell(2, 2).Value = relatorio.TipoRelatorio;

        worksheet.Cell(3, 1).Value = "Período:";
        worksheet.Cell(3, 2).Value = relatorio.Periodo;

        worksheet.Cell(4, 1).Value = "Data Geração:";
        worksheet.Cell(4, 2).Value = relatorio.DataGeracao;

        worksheet.Cell(5, 1).Value = "Total Registros:";
        worksheet.Cell(5, 2).Value = relatorio.TotalRegistros;

        // Resumo Executivo
        int linha = 7;
        worksheet.Cell(linha, 1).Value = "Resumo Executivo";
        worksheet.Cell(linha, 1).Style.Font.Bold = true;
        linha++;

        worksheet.Cell(linha, 1).Value = "Usuários Ativos:";
        worksheet.Cell(linha, 2).Value = relatorio.Resumo.TotalUsuariosAtivos;
        linha++;

        worksheet.Cell(linha, 1).Value = "Recursos Afetados:";
        worksheet.Cell(linha, 2).Value = relatorio.Resumo.TotalRecursosAfetados;
        linha++;

        worksheet.Cell(linha, 1).Value = "Ação Mais Frequente:";
        worksheet.Cell(linha, 2).Value = relatorio.Resumo.AcaoMaisFrequente;
        linha++;

        worksheet.Cell(linha, 1).Value = "Recurso Mais Alterado:";
        worksheet.Cell(linha, 2).Value = relatorio.Resumo.RecursoMaisAlterado;
        linha++;

        worksheet.Cell(linha, 1).Value = "Horário Pico:";
        worksheet.Cell(linha, 2).Value = relatorio.Resumo.HorarioPicoAtividade;

        worksheet.ColumnsUsed().AdjustToContents();
    }

    /// <summary>
    /// Cria planilha de estatísticas no Excel
    /// </summary>
    private void CriarPlanilhaEstatisticas(IXLWorksheet worksheet, EstatisticasAuditoria estatisticas)
    {
        worksheet.Cell(1, 1).Value = "Estatísticas de Auditoria";
        worksheet.Cell(1, 1).Style.Font.Bold = true;

        int linha = 3;

        // Ações mais frequentes
        worksheet.Cell(linha, 1).Value = "Ações Mais Frequentes";
        worksheet.Cell(linha, 1).Style.Font.Bold = true;
        linha++;

        worksheet.Cell(linha, 1).Value = "Ação";
        worksheet.Cell(linha, 2).Value = "Quantidade";
        worksheet.Cell(linha, 1).Style.Font.Bold = true;
        worksheet.Cell(linha, 2).Style.Font.Bold = true;
        linha++;

        foreach (var acao in estatisticas.AcoesMaisFrequentes.Take(10))
        {
            worksheet.Cell(linha, 1).Value = acao.Key;
            worksheet.Cell(linha, 2).Value = acao.Value;
            linha++;
        }

        linha += 2;

        // Recursos mais acessados
        worksheet.Cell(linha, 1).Value = "Recursos Mais Acessados";
        worksheet.Cell(linha, 1).Style.Font.Bold = true;
        linha++;

        worksheet.Cell(linha, 1).Value = "Recurso";
        worksheet.Cell(linha, 2).Value = "Acessos";
        worksheet.Cell(linha, 1).Style.Font.Bold = true;
        worksheet.Cell(linha, 2).Style.Font.Bold = true;
        linha++;

        foreach (var recurso in estatisticas.RecursosMaisAcessados.Take(10))
        {
            worksheet.Cell(linha, 1).Value = recurso.Key;
            worksheet.Cell(linha, 2).Value = recurso.Value;
            linha++;
        }

        worksheet.ColumnsUsed().AdjustToContents();
    }

    /// <summary>
    /// Cria planilha de dados no Excel
    /// </summary>
    private void CriarPlanilhaDados(IXLWorksheet worksheet, object dados, string tipoRelatorio)
    {
        worksheet.Cell(1, 1).Value = $"Dados - {tipoRelatorio}";
        worksheet.Cell(1, 1).Style.Font.Bold = true;

        // Implementação básica - em produção, seria mais específica por tipo de relatório
        worksheet.Cell(3, 1).Value = "Dados detalhados disponíveis via API JSON";
        worksheet.ColumnsUsed().AdjustToContents();
    }

    /// <summary>
    /// Gera HTML para relatório
    /// </summary>
    private string GerarHtmlRelatorio(RelatorioAuditoriaResponse relatorio)
    {
        var html = new StringBuilder();
        html.AppendLine("<html><head><title>Relatório de Auditoria</title>");
        html.AppendLine("<style>body{font-family:Arial,sans-serif;margin:20px;}table{border-collapse:collapse;width:100%;}th,td{border:1px solid #ddd;padding:8px;text-align:left;}th{background-color:#f2f2f2;}</style>");
        html.AppendLine("</head><body>");
        
        html.AppendLine($"<h1>Relatório de Auditoria - {relatorio.TipoRelatorio}</h1>");
        html.AppendLine($"<p><strong>Período:</strong> {relatorio.Periodo}</p>");
        html.AppendLine($"<p><strong>Gerado em:</strong> {relatorio.DataGeracao:dd/MM/yyyy HH:mm:ss}</p>");
        html.AppendLine($"<p><strong>Total de Registros:</strong> {relatorio.TotalRegistros}</p>");

        html.AppendLine("<h2>Resumo Executivo</h2>");
        html.AppendLine("<table>");
        html.AppendLine($"<tr><td>Usuários Ativos</td><td>{relatorio.Resumo.TotalUsuariosAtivos}</td></tr>");
        html.AppendLine($"<tr><td>Recursos Afetados</td><td>{relatorio.Resumo.TotalRecursosAfetados}</td></tr>");
        html.AppendLine($"<tr><td>Ação Mais Frequente</td><td>{relatorio.Resumo.AcaoMaisFrequente}</td></tr>");
        html.AppendLine($"<tr><td>Recurso Mais Alterado</td><td>{relatorio.Resumo.RecursoMaisAlterado}</td></tr>");
        html.AppendLine($"<tr><td>Horário Pico</td><td>{relatorio.Resumo.HorarioPicoAtividade}</td></tr>");
        html.AppendLine("</table>");

        if (relatorio.Resumo.AlertasCompliance.Any())
        {
            html.AppendLine("<h2>Alertas de Compliance</h2>");
            html.AppendLine("<ul>");
            foreach (var alerta in relatorio.Resumo.AlertasCompliance)
            {
                html.AppendLine($"<li>{alerta}</li>");
            }
            html.AppendLine("</ul>");
        }

        html.AppendLine("</body></html>");
        return html.ToString();
    }

    #endregion

}