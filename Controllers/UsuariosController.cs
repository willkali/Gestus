using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using OpenIddict.Abstractions;
using Gestus.Modelos;
using Gestus.Dados;
using Gestus.DTOs.Usuario;
using Gestus.DTOs.Comuns;
using Gestus.Validadores;
using Gestus.Services;
using System.Text.Json;

namespace Gestus.Controllers;

/// <summary>
/// Controller para gerenciamento completo de usuários
/// Implementa operações CRUD robustas com paginação, filtros avançados e auditoria
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class UsuariosController : ControllerBase
{
    private readonly UserManager<Usuario> _userManager;
    private readonly RoleManager<Papel> _roleManager;
    private readonly GestusDbContexto _context;
    private readonly ILogger<UsuariosController> _logger;
    private readonly IArquivoService _arquivoService;
    private readonly IEmailService _emailService;
    private int? papeisParaAdicionar;

    public UsuariosController(
        UserManager<Usuario> userManager,
        RoleManager<Papel> roleManager,
        GestusDbContexto context,
        ILogger<UsuariosController> logger,
        IArquivoService arquivoService,
        IEmailService emailService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _logger = logger;
        _arquivoService = arquivoService;
        _emailService = emailService;
    }

    /// <summary>
    /// Lista usuários com paginação, filtros e ordenação avançada
    /// </summary>
    /// <param name="filtros">Filtros de busca</param>
    /// <returns>Lista paginada de usuários</returns>
    [HttpGet]
    [ProducesResponseType(typeof(RespostaPaginada<UsuarioResumo>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListarUsuarios([FromQuery] FiltrosUsuario filtros)
    {
        try
        {
            // ✅ VERIFICAR PERMISSÃO ESPECÍFICA
            if (!TemPermissao("Usuarios.Listar"))
            {
                return Forbid("Permissão insuficiente para listar usuários");
            }

            var usuarioLogadoId = ObterUsuarioLogadoId();
            _logger.LogInformation("🔍 Listando usuários - Solicitado por: {UsuarioId}, Filtros: {@Filtros}",
                usuarioLogadoId, filtros);

            // ✅ CONSTRUIR QUERY DINÂMICA COM PERFORMANCE
            var query = _context.Users.AsQueryable();

            // ✅ APLICAR FILTROS AVANÇADOS
            query = AplicarFiltros(query, filtros);

            // ✅ INCLUIR RELACIONAMENTOS NECESSÁRIOS (otimizado)
            query = query.Include(u => u.UsuarioPapeis)
                         .ThenInclude(up => up.Papel)
                         .AsSplitQuery(); // ✅ Evitar cartesian explosion

            // ✅ CONTAR TOTAL ANTES DA PAGINAÇÃO (otimizado)
            var totalItens = await query.CountAsync();

            // ✅ APLICAR ORDENAÇÃO DINÂMICA
            query = AplicarOrdenacao(query, filtros.OrdenarPor, filtros.DirecaoOrdenacao);

            // ✅ APLICAR PAGINAÇÃO OTIMIZADA
            var usuarios = await query
                .Skip((filtros.Pagina - 1) * filtros.ItensPorPagina)
                .Take(filtros.ItensPorPagina)
                .Select(u => new UsuarioResumo
                {
                    Id = u.Id,
                    Email = u.Email!,
                    Nome = u.Nome,
                    Sobrenome = u.Sobrenome,
                    NomeCompleto = u.NomeCompleto ?? $"{u.Nome} {u.Sobrenome}",
                    Ativo = u.Ativo,
                    DataCriacao = u.DataCriacao,
                    UltimoLogin = u.UltimoLogin,
                    Papeis = u.UsuarioPapeis
                        .Where(up => up.Ativo && up.Papel.Ativo)
                        .Select(up => up.Papel.Name!)
                        .ToList(),
                    TotalPermissoes = u.UsuarioPapeis
                        .Where(up => up.Ativo && up.Papel.Ativo)
                        .SelectMany(up => up.Papel.PapelPermissoes)
                        .Where(pp => pp.Ativo)
                        .Count()
                })
                .ToListAsync();

            // ✅ REGISTRAR AUDITORIA
            await RegistrarAuditoria("Usuarios.Listar", "Usuarios", null,
                $"Listados {usuarios.Count} usuários com filtros: {filtros}");

            var resposta = new RespostaPaginada<UsuarioResumo>
            {
                Dados = usuarios,
                PaginaAtual = filtros.Pagina,
                ItensPorPagina = filtros.ItensPorPagina,
                TotalItens = totalItens,
                TotalPaginas = (int)Math.Ceiling((double)totalItens / filtros.ItensPorPagina),
                TemProximaPagina = filtros.Pagina * filtros.ItensPorPagina < totalItens,
                TemPaginaAnterior = filtros.Pagina > 1
            };

            _logger.LogInformation("✅ Usuários listados - Total: {Total}, Página: {Pagina}/{TotalPaginas}",
                totalItens, filtros.Pagina, resposta.TotalPaginas);

            return Ok(resposta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao listar usuários");
            return StatusCode(500, new RespostaErro
            {
                Erro = "ErroInterno",
                Mensagem = "Erro interno ao listar usuários"
            });
        }
    }

    /// <summary>
    /// Obtém detalhes completos de um usuário específico
    /// </summary>
    /// <param name="id">ID do usuário</param>
    /// <returns>Detalhes completos do usuário</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UsuarioCompleto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ObterUsuario(int id)
    {
        try
        {
            var usuarioLogadoId = ObterUsuarioLogadoId();

            // ✅ AUTORIZAÇÃO CONTEXTUAL - pode ver próprio perfil OU ter permissão
            var podeVisualizar = usuarioLogadoId == id || TemPermissao("Usuarios.Visualizar");

            if (!podeVisualizar)
            {
                return Forbid("Permissão insuficiente para visualizar este usuário");
            }

            _logger.LogInformation("🔍 Buscando usuário ID: {Id} - Solicitado por: {UsuarioLogadoId}",
                id, usuarioLogadoId);

            // ✅ BUSCA OTIMIZADA COM TODOS OS RELACIONAMENTOS
            var usuario = await _context.Users
                .Include(u => u.UsuarioPapeis.Where(up => up.Ativo))
                    .ThenInclude(up => up.Papel)
                        .ThenInclude(p => p.PapelPermissoes.Where(pp => pp.Ativo))
                            .ThenInclude(pp => pp.Permissao)
                .Include(u => u.UsuarioGrupos.Where(ug => ug.Ativo))
                    .ThenInclude(ug => ug.Grupo)
                .AsSplitQuery()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (usuario == null)
            {
                _logger.LogWarning("⚠️ Usuário não encontrado: {Id}", id);
                return NotFound(new RespostaErro
                {
                    Erro = "UsuarioNaoEncontrado",
                    Mensagem = "Usuário não encontrado"
                });
            }

            // ✅ CONSTRUIR RESPOSTA DETALHADA
            var papeis = usuario.UsuarioPapeis
                .Where(up => up.Ativo && up.Papel.Ativo)
                .Select(up => new PapelUsuario
                {
                    Id = up.Papel.Id,
                    Nome = up.Papel.Name!,
                    Descricao = up.Papel.Descricao,
                    Categoria = up.Papel.Categoria,
                    DataAtribuicao = up.DataAtribuicao,
                    DataExpiracao = up.DataExpiracao
                })
                .ToList();

            var permissoes = usuario.UsuarioPapeis
                .Where(up => up.Ativo && up.Papel.Ativo)
                .SelectMany(up => up.Papel.PapelPermissoes)
                .Where(pp => pp.Ativo && pp.Permissao.Ativo)
                .Select(pp => new PermissaoUsuario
                {
                    Id = pp.Permissao.Id,
                    Nome = pp.Permissao.Nome,
                    Descricao = pp.Permissao.Descricao,
                    Recurso = pp.Permissao.Recurso,
                    Acao = pp.Permissao.Acao,
                    Categoria = pp.Permissao.Categoria,
                    OrigemPapel = pp.Papel.Name!
                })
                .DistinctBy(p => p.Nome)
                .ToList();

            var grupos = usuario.UsuarioGrupos
                .Where(ug => ug.Ativo && ug.Grupo.Ativo)
                .Select(ug => new GrupoUsuario
                {
                    Id = ug.Grupo.Id,
                    Nome = ug.Grupo.Nome,
                    Descricao = ug.Grupo.Descricao,
                    Tipo = ug.Grupo.Tipo,
                    DataAdesao = ug.DataAdesao
                })
                .ToList();

            var usuarioCompleto = new UsuarioCompleto
            {
                Id = usuario.Id,
                Email = usuario.Email!,
                Nome = usuario.Nome,
                Sobrenome = usuario.Sobrenome,
                NomeCompleto = usuario.NomeCompleto ?? $"{usuario.Nome} {usuario.Sobrenome}",
                Telefone = usuario.PhoneNumber,
                EmailConfirmado = usuario.EmailConfirmed,
                TelefoneConfirmado = usuario.PhoneNumberConfirmed,
                Ativo = usuario.Ativo,
                Observacoes = usuario.Observacoes,
                DataCriacao = usuario.DataCriacao,
                DataAtualizacao = usuario.DataAtualizacao,
                UltimoLogin = usuario.UltimoLogin,
                Papeis = papeis,
                Permissoes = permissoes,
                Grupos = grupos,
                Estatisticas = new EstatisticasUsuario
                {
                    TotalPapeis = papeis.Count,
                    TotalPermissoes = permissoes.Count,
                    TotalGrupos = grupos.Count,
                    ContadorLogins = await ContarLoginsUsuario(id),
                    UltimoPapelAtribuido = papeis.OrderByDescending(p => p.DataAtribuicao).FirstOrDefault()?.DataAtribuicao
                }
            };

            // ✅ REGISTRAR AUDITORIA
            await RegistrarAuditoria("Usuarios.Visualizar", "Usuarios", id.ToString(),
                $"Visualizado usuário: {usuario.Email}");

            _logger.LogInformation("✅ Usuário obtido - ID: {Id}, Email: {Email}, Papéis: {TotalPapeis}, Permissões: {TotalPermissoes}",
                id, usuario.Email, papeis.Count, permissoes.Count);

            return Ok(usuarioCompleto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao obter usuário ID: {Id}", id);
            return StatusCode(500, new RespostaErro
            {
                Erro = "ErroInterno",
                Mensagem = "Erro interno ao obter usuário"
            });
        }
    }

    /// <summary>
    /// Cria um novo usuário no sistema
    /// </summary>
    /// <param name="request">Dados do novo usuário</param>
    /// <returns>Usuário criado</returns>
    [HttpPost]
    [ProducesResponseType(typeof(UsuarioCompleto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CriarUsuario([FromBody] CriarUsuarioRequest request)
    {
        try
        {
            if (!TemPermissao("Usuarios.Criar"))
            {
                return Forbid("Acesso negado para criar usuários");
            }

            // ✅ VALIDAÇÃO CORRIGIDA - passar UserManager
            var validator = new CriarUsuarioValidator(_userManager);
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(new RespostaErro
                {
                    Erro = "DadosInvalidos",
                    Mensagem = "Dados fornecidos são inválidos",
                    Detalhes = validationResult.Errors.Select(e => e.ErrorMessage).ToList()
                });
            }

            var usuarioLogadoId = ObterUsuarioLogadoId();

            // Verificar se email já existe
            var usuarioExistente = await _userManager.FindByEmailAsync(request.Email);
            if (usuarioExistente != null)
            {
                return BadRequest(new RespostaErro
                {
                    Erro = "EmailJaExiste",
                    Mensagem = "Este endereço de email já está em uso"
                });
            }

            // Criar usuário
            var usuario = new Usuario
            {
                Email = request.Email,
                UserName = request.Email,
                Nome = request.Nome,
                Sobrenome = request.Sobrenome,
                NomeCompleto = $"{request.Nome} {request.Sobrenome}",
                PhoneNumber = request.Telefone,
                EmailConfirmed = request.ConfirmarEmailImediatamente,
                PhoneNumberConfirmed = request.ConfirmarTelefoneImediatamente,
                Ativo = request.Ativo,
                DataCriacao = DateTime.UtcNow,
                Observacoes = request.Observacoes
            };

            var resultado = await _userManager.CreateAsync(usuario, request.Senha);
            if (!resultado.Succeeded)
            {
                return BadRequest(new RespostaErro
                {
                    Erro = "ErroCreacion",
                    Mensagem = "Erro ao criar usuário",
                    Detalhes = resultado.Errors.Select(e => e.Description).ToList()
                });
            }

            // Atribuir papéis se especificados
            if (request.Papeis?.Any() == true)
            {
                var papeisValidos = await _roleManager.Roles
                    .Where(r => request.Papeis.Contains(r.Name!) && r.Ativo)
                    .Select(r => r.Name!)
                    .ToListAsync();

                if (papeisValidos.Any())
                {
                    await _userManager.AddToRolesAsync(usuario, papeisValidos);
                    await AtualizarMetadadosPapeis(usuario.Id, papeisValidos, usuarioLogadoId);
                }
            }

            // ✅ DECLARAR VARIÁVEL FORA DO TRY PARA USAR NA AUDITORIA
            bool emailEnviado = false;

            // ✅ ENVIO DE EMAIL DE BOAS-VINDAS
            try
            {
                emailEnviado = await _emailService.EnviarEmailBoasVindasAsync(
                    emailDestino: usuario.Email!,
                    nomeUsuario: usuario.Nome
                );

                if (emailEnviado)
                {
                    _logger.LogInformation("✅ Email de boas-vindas enviado para {Email}", usuario.Email);
                }
                else
                {
                    _logger.LogWarning("⚠️ Falha ao enviar email de boas-vindas para {Email}", usuario.Email);
                }
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx, "❌ Erro ao enviar email de boas-vindas para {Email}", usuario.Email);
                // Não falha a criação do usuário por causa do email
                emailEnviado = false;
            }

            // Registrar auditoria
            await RegistrarAuditoria(
                acao: "Criar",
                recurso: "Usuario",
                recursoId: usuario.Id.ToString(),
                observacoes: $"Usuário {usuario.Email} criado com sucesso",
                dadosAntes: null,
                dadosDepois: new
                {
                    usuario.Id,
                    usuario.Email,
                    usuario.Nome,
                    usuario.Sobrenome,
                    Papeis = request.Papeis,
                    EmailEnviado = emailEnviado // ✅ AGORA A VARIÁVEL ESTÁ NO ESCOPO CORRETO
                }
            );

            var usuarioCompleto = await ObterUsuarioCompletoAsync(usuario.Id);

            return CreatedAtAction(
                nameof(ObterUsuario),
                new { id = usuario.Id },
                usuarioCompleto
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao criar usuário");
            return StatusCode(500, new RespostaErro
            {
                Erro = "ErroInterno",
                Mensagem = "Erro interno do servidor"
            });
        }
    }

    /// <summary>
    /// Atualiza um usuário existente
    /// </summary>
    /// <param name="id">ID do usuário a ser atualizado</param>
    /// <param name="request">Dados de atualização do usuário</param>
    /// <returns>Usuário atualizado</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(UsuarioCompleto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AtualizarUsuario(int id, [FromBody] AtualizarUsuarioRequest request)
    {
        try
        {
            if (!TemPermissao("Usuarios.Editar"))
            {
                return Forbid();
            }

            var validator = new AtualizarUsuarioValidator(_userManager);
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(new RespostaErro 
                { 
                    Erro = "Dados inválidos", 
                    Mensagem = "Os dados fornecidos são inválidos",
                    Detalhes = validationResult.Errors.Select(e => e.ErrorMessage).ToList()
                });
            }

            // ✅ CORRIGIDO: Usar ExecutionStrategy para transações
            var strategy = _context.Database.CreateExecutionStrategy();
            var resultado = await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var usuario = await _userManager.FindByIdAsync(id.ToString());
                    if (usuario == null)
                    {
                        throw new InvalidOperationException("Usuário não encontrado");
                    }

                    // Capturar dados antes da atualização para auditoria
                    var dadosAntes = JsonSerializer.Serialize(new
                    {
                        usuario.Email,
                        usuario.Nome,
                        usuario.Sobrenome,
                        usuario.PhoneNumber,
                        usuario.Ativo
                    });

                    // Verificar unicidade do email se foi alterado
                    if (!string.IsNullOrEmpty(request.Email) && request.Email != usuario.Email)
                    {
                        var usuarioExistente = await _userManager.FindByEmailAsync(request.Email);
                        if (usuarioExistente != null)
                        {
                            throw new InvalidOperationException("Já existe um usuário com este email");
                        }
                    }

                    // Atualizar campos
                    if (!string.IsNullOrEmpty(request.Email))
                    {
                        usuario.Email = request.Email;
                        usuario.UserName = request.Email;
                    }
                    if (!string.IsNullOrEmpty(request.Nome))
                        usuario.Nome = request.Nome;
                    if (!string.IsNullOrEmpty(request.Sobrenome))
                        usuario.Sobrenome = request.Sobrenome;
                    if (!string.IsNullOrEmpty(request.Telefone))
                        usuario.PhoneNumber = request.Telefone;
                    if (request.Ativo.HasValue)
                        usuario.Ativo = request.Ativo.Value;
                    if (!string.IsNullOrEmpty(request.Observacoes))
                        usuario.Observacoes = request.Observacoes;

                    usuario.NomeCompleto = $"{usuario.Nome} {usuario.Sobrenome}";
                    usuario.DataAtualizacao = DateTime.UtcNow;

                    if (request.ConfirmarEmailImediatamente)
                        usuario.EmailConfirmed = true;
                    if (request.ConfirmarTelefoneImediatamente)
                        usuario.PhoneNumberConfirmed = true;

                    // Atualizar senha se fornecida
                    if (!string.IsNullOrEmpty(request.NovaSenha))
                    {
                        var token = await _userManager.GeneratePasswordResetTokenAsync(usuario);
                        var resultadoSenha = await _userManager.ResetPasswordAsync(usuario, token, request.NovaSenha);
                        if (!resultadoSenha.Succeeded)
                        {
                            throw new InvalidOperationException($"Erro ao atualizar senha: {string.Join(", ", resultadoSenha.Errors.Select(e => e.Description))}");
                        }
                    }

                    var resultadoAtualizacao = await _userManager.UpdateAsync(usuario);
                    if (!resultadoAtualizacao.Succeeded)
                    {
                        throw new InvalidOperationException($"Erro ao atualizar usuário: {string.Join(", ", resultadoAtualizacao.Errors.Select(e => e.Description))}");
                    }

                    // Gerenciar papéis se fornecidos
                    if (request.Papeis != null)
                    {
                        var papeisAtuais = await _userManager.GetRolesAsync(usuario);
                        await _userManager.RemoveFromRolesAsync(usuario, papeisAtuais);

                        var papeisValidos = new List<string>();
                        foreach (var papel in request.Papeis)
                        {
                            if (await _roleManager.RoleExistsAsync(papel))
                            {
                                papeisValidos.Add(papel);
                            }
                        }

                        if (papeisValidos.Any())
                        {
                            await _userManager.AddToRolesAsync(usuario, papeisValidos);
                        }
                    }

                    // Registrar auditoria
                    var dadosDepois = JsonSerializer.Serialize(new
                    {
                        usuario.Email,
                        usuario.Nome,
                        usuario.Sobrenome,
                        usuario.PhoneNumber,
                        usuario.Ativo
                    });

                    var usuarioLogadoId = ObterUsuarioLogadoId();
                    var registroAuditoria = new RegistroAuditoria
                    {
                        UsuarioId = usuarioLogadoId,
                        Acao = "Atualizar",
                        Recurso = "Usuario",
                        RecursoId = usuario.Id.ToString(),
                        DadosAntes = dadosAntes,
                        DadosDepois = dadosDepois,
                        DataHora = DateTime.UtcNow,
                        EnderecoIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
                        UserAgent = Request.Headers.UserAgent.ToString(),
                        Observacoes = $"Usuário {usuario.Email} atualizado"
                    };

                    _context.RegistrosAuditoria.Add(registroAuditoria);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    return usuario;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });

            var usuarioCompleto = await ObterUsuarioCompletoAsync(resultado.Id);
            return Ok(usuarioCompleto);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("não encontrado"))
        {
            return NotFound(new RespostaErro { Erro = "Usuário não encontrado", Mensagem = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new RespostaErro { Erro = "Dados inválidos", Mensagem = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Erro ao atualizar usuário: {id}");
            return StatusCode(500, new RespostaErro 
            { 
                Erro = "Erro interno", 
                Mensagem = "Erro ao atualizar usuário" 
            });
        }
    }

    /// <summary>
    /// Remove um usuário do sistema (soft delete por padrão)
    /// </summary>
    /// <param name="id">ID do usuário a ser removido</param>
    /// <param name="exclusaoPermanente">Se true, faz hard delete (exclusão definitiva)</param>
    /// <returns>Confirmação da remoção</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(RespostaSucesso), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoverUsuario(int id, [FromQuery] bool exclusaoPermanente = false)
    {
        try
        {
            var usuarioLogadoId = ObterUsuarioLogadoId();

            // ✅ VERIFICAR PERMISSÕES ESPECÍFICAS
            var podeDesativar = TemPermissao("Usuarios.Desativar");
            var podeExcluir = TemPermissao("Usuarios.Excluir") || TemPermissao("Sistema.Controle.Total");

            if (exclusaoPermanente && !podeExcluir)
            {
                return Forbid("Permissão insuficiente para exclusão permanente de usuários");
            }

            if (!exclusaoPermanente && !podeDesativar)
            {
                return Forbid("Permissão insuficiente para desativar usuários");
            }

            _logger.LogInformation("🗑️ {TipoOperacao} usuário ID: {Id} - Solicitado por: {UsuarioLogadoId}",
                exclusaoPermanente ? "Excluindo permanentemente" : "Desativando", id, usuarioLogadoId);

            // ✅ BUSCAR USUÁRIO EXISTENTE COM RELACIONAMENTOS
            var usuarioExistente = await _context.Users
                .Include(u => u.UsuarioPapeis.Where(up => up.Ativo))
                    .ThenInclude(up => up.Papel)
                .Include(u => u.UsuarioGrupos.Where(ug => ug.Ativo))
                    .ThenInclude(ug => ug.Grupo)
                .Include(u => u.RegistrosAuditoria.Take(5))
                .FirstOrDefaultAsync(u => u.Id == id);

            if (usuarioExistente == null)
            {
                _logger.LogWarning("⚠️ Usuário não encontrado para remoção: {Id}", id);
                return NotFound(new RespostaErro
                {
                    Erro = "UsuarioNaoEncontrado",
                    Mensagem = "Usuário não encontrado"
                });
            }

            // ✅ VALIDAÇÕES DE SEGURANÇA
            var validacaoResult = await ValidarRemocaoUsuario(usuarioExistente, usuarioLogadoId, exclusaoPermanente);
            if (validacaoResult != null)
            {
                return validacaoResult;
            }

            // ✅ CAPTURAR DADOS ANTES DA REMOÇÃO (para auditoria)
            var dadosAntes = new
            {
                Email = usuarioExistente.Email,
                Nome = usuarioExistente.Nome,
                Sobrenome = usuarioExistente.Sobrenome,
                Ativo = usuarioExistente.Ativo,
                Papeis = usuarioExistente.UsuarioPapeis
                    .Where(up => up.Ativo)
                    .Select(up => up.Papel.Name)
                    .ToList(),
                Grupos = usuarioExistente.UsuarioGrupos
                    .Where(ug => ug.Ativo)
                    .Select(ug => ug.Grupo.Nome)
                    .ToList(),
                TotalRegistrosAuditoria = await _context.RegistrosAuditoria
                    .Where(ra => ra.UsuarioId == id)
                    .CountAsync()
            };

            // ✅ CORRIGIDO: Usar ExecutionStrategy para transações
            var strategy = _context.Database.CreateExecutionStrategy();
            var resultado = await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    string mensagemSucesso;
                    string acaoAuditoria;

                    if (exclusaoPermanente)
                    {
                        // ✅ HARD DELETE - EXCLUSÃO PERMANENTE
                        await RealizarExclusaoPermanente(usuarioExistente);
                        mensagemSucesso = $"Usuário {usuarioExistente.Email} excluído permanentemente do sistema";
                        acaoAuditoria = "Usuarios.ExcluirPermanentemente";
                    }
                    else
                    {
                        // ✅ SOFT DELETE - DESATIVAÇÃO
                        await RealizarDesativacao(usuarioExistente);
                        mensagemSucesso = $"Usuário {usuarioExistente.Email} desativado com sucesso";
                        acaoAuditoria = "Usuarios.Desativar";
                    }

                    // ✅ REGISTRAR AUDITORIA DENTRO DA TRANSAÇÃO
                    var registroAuditoria = new RegistroAuditoria
                    {
                        UsuarioId = usuarioLogadoId,
                        Acao = acaoAuditoria,
                        Recurso = "Usuarios",
                        RecursoId = id.ToString(),
                        DadosAntes = System.Text.Json.JsonSerializer.Serialize(dadosAntes),
                        DadosDepois = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            ExclusaoPermanente = exclusaoPermanente,
                            DataOperacao = DateTime.UtcNow
                        }),
                        DataHora = DateTime.UtcNow,
                        EnderecoIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
                        UserAgent = Request.Headers.UserAgent.ToString(),
                        Observacoes = mensagemSucesso
                    };

                    _context.RegistrosAuditoria.Add(registroAuditoria);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    return mensagemSucesso;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });

            _logger.LogInformation("✅ {TipoOperacao} realizada - ID: {Id}, Email: {Email}",
                exclusaoPermanente ? "Exclusão permanente" : "Desativação", id, usuarioExistente.Email);

            return Ok(new RespostaSucesso
            {
                Sucesso = true,
                Mensagem = resultado
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao {TipoOperacao} usuário ID: {Id}",
                exclusaoPermanente ? "excluir permanentemente" : "desativar", id);

            return StatusCode(500, new RespostaErro
            {
                Erro = "ErroInterno",
                Mensagem = $"Erro interno ao {(exclusaoPermanente ? "excluir" : "desativar")} usuário"
            });
        }
    }

    /// <summary>
    /// Reativa um usuário desativado
    /// </summary>
    /// <param name="id">ID do usuário a ser reativado</param>
    /// <returns>Confirmação da reativação</returns>
    [HttpPost("{id:int}/reativar")]
    [ProducesResponseType(typeof(UsuarioCompleto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ReativarUsuario(int id)
    {
        try
        {
            // ✅ VERIFICAR PERMISSÃO
            if (!TemPermissao("Usuarios.Reativar"))
            {
                return Forbid("Permissão insuficiente para reativar usuários");
            }

            var usuarioLogadoId = ObterUsuarioLogadoId();
            _logger.LogInformation("🔄 Reativando usuário ID: {Id} - Solicitado por: {UsuarioLogadoId}",
                id, usuarioLogadoId);

            // ✅ BUSCAR USUÁRIO DESATIVADO
            var usuarioDesativado = await _context.Users
                .Include(u => u.UsuarioPapeis)
                    .ThenInclude(up => up.Papel)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (usuarioDesativado == null)
            {
                return NotFound(new RespostaErro
                {
                    Erro = "UsuarioNaoEncontrado",
                    Mensagem = "Usuário não encontrado"
                });
            }

            if (usuarioDesativado.Ativo)
            {
                return BadRequest(new RespostaErro
                {
                    Erro = "UsuarioJaAtivo",
                    Mensagem = "Usuário já está ativo"
                });
            }

            // ✅ CAPTURAR DADOS ANTES DA REATIVAÇÃO
            var dadosAntes = new
            {
                Email = usuarioDesativado.Email,
                Ativo = usuarioDesativado.Ativo,
                DataAtualizacao = usuarioDesativado.DataAtualizacao
            };

            // ✅ REATIVAR USUÁRIO
            usuarioDesativado.Ativo = true;
            usuarioDesativado.DataAtualizacao = DateTime.UtcNow;

            var resultado = await _userManager.UpdateAsync(usuarioDesativado);

            if (!resultado.Succeeded)
            {
                return BadRequest(new RespostaErro
                {
                    Erro = "ErroReativacao",
                    Mensagem = "Erro ao reativar usuário",
                    Detalhes = resultado.Errors.Select(e => e.Description).ToList()
                });
            }

            // ✅ REGISTRAR AUDITORIA
            await RegistrarAuditoria("Usuarios.Reativar", "Usuarios", id.ToString(),
                $"Usuário reativado: {usuarioDesativado.Email}", dadosAntes, new
                {
                    Email = usuarioDesativado.Email,
                    Ativo = usuarioDesativado.Ativo,
                    DataReativacao = usuarioDesativado.DataAtualizacao
                });

            _logger.LogInformation("✅ Usuário reativado - ID: {Id}, Email: {Email}",
                id, usuarioDesativado.Email);

            // ✅ RETORNAR DADOS COMPLETOS ATUALIZADOS
            var usuarioCompleto = await ConstruirUsuarioCompleto(usuarioDesativado);

            return Ok(usuarioCompleto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao reativar usuário ID: {Id}", id);
            return StatusCode(500, new RespostaErro
            {
                Erro = "ErroInterno",
                Mensagem = "Erro interno ao reativar usuário"
            });
        }
    }

    /// <summary>
    /// Gerencia papéis de um usuário específico
    /// </summary>
    /// <param name="id">ID do usuário</param>
    /// <param name="request">Dados para gerenciamento de papéis</param>
    /// <returns>Papéis atualizados do usuário</returns>
    [HttpPost("{id:int}/papeis")]
    [ProducesResponseType(typeof(RespostaGerenciamentoPapeis), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GerenciarPapeisUsuario(int id, [FromBody] GerenciarPapeisRequest request)
    {
        try
        {
            var usuarioLogadoId = ObterUsuarioLogadoId();

            // ✅ VERIFICAR PERMISSÃO ESPECÍFICA
            if (!TemPermissao("Usuarios.GerenciarPapeis"))
            {
                return Forbid("Permissão insuficiente para gerenciar papéis de usuários");
            }

            _logger.LogInformation("🎭 Gerenciando papéis do usuário ID: {Id} - Solicitado por: {UsuarioLogadoId}, Operação: {Operacao}",
                id, usuarioLogadoId, request.Operacao);

            // ✅ VALIDAÇÃO AVANÇADA
            var validador = new GerenciarPapeisValidator(_roleManager);
            var resultadoValidacao = await validador.ValidateAsync(request);

            if (!resultadoValidacao.IsValid)
            {
                return BadRequest(new RespostaErro
                {
                    Erro = "DadosInvalidos",
                    Mensagem = "Dados de gerenciamento de papéis são inválidos",
                    Detalhes = resultadoValidacao.Errors.Select(e => e.ErrorMessage).ToList()
                });
            }

            // ✅ BUSCAR USUÁRIO EXISTENTE
            var usuario = await _context.Users
                .Include(u => u.UsuarioPapeis.Where(up => up.Ativo))
                    .ThenInclude(up => up.Papel)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (usuario == null)
            {
                _logger.LogWarning("⚠️ Usuário não encontrado para gerenciamento de papéis: {Id}", id);
                return NotFound(new RespostaErro
                {
                    Erro = "UsuarioNaoEncontrado",
                    Mensagem = "Usuário não encontrado"
                });
            }

            // ✅ VALIDAÇÕES DE SEGURANÇA
            var validacaoSeguranca = await ValidarOperacaoPapeis(usuario, request, usuarioLogadoId);
            if (validacaoSeguranca != null)
            {
                return validacaoSeguranca;
            }

            // ✅ CAPTURAR DADOS ANTES DA OPERAÇÃO
            var papeisAntes = usuario.UsuarioPapeis
                .Where(up => up.Ativo)
                .Select(up => new {
                    Nome = up.Papel.Name,
                    Id = up.Papel.Id,
                    DataAtribuicao = up.DataAtribuicao,
                    AtribuidoPorId = up.AtribuidoPorId
                })
                .ToList();

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var resultado = request.Operacao.ToLower() switch
                {
                    "substituir" => await SubstituirPapeis(usuario, request.Papeis, usuarioLogadoId),
                    "adicionar" => await AdicionarPapeis(usuario, request.Papeis, usuarioLogadoId),
                    "remover" => await RemoverPapeis(usuario, request.Papeis, usuarioLogadoId),
                    "limpar" => await LimparTodosPapeis(usuario, usuarioLogadoId),
                    _ => throw new ArgumentException($"Operação inválida: {request.Operacao}")
                };

                if (!resultado.Sucesso)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new RespostaErro
                    {
                        Erro = "ErroOperacaoPapeis",
                        Mensagem = resultado.Mensagem,
                        Detalhes = resultado.Detalhes
                    });
                }

                // ✅ ATUALIZAR TIMESTAMP DO USUÁRIO
                usuario.DataAtualizacao = DateTime.UtcNow;
                await _userManager.UpdateAsync(usuario);

                // ✅ COMMIT DA TRANSAÇÃO
                await transaction.CommitAsync();

                // ✅ RECARREGAR USUÁRIO COM PAPÉIS ATUALIZADOS
                var usuarioAtualizado = await _context.Users
                    .Include(u => u.UsuarioPapeis.Where(up => up.Ativo))
                        .ThenInclude(up => up.Papel)
                            .ThenInclude(p => p.PapelPermissoes.Where(pp => pp.Ativo))
                                .ThenInclude(pp => pp.Permissao)
                    .FirstAsync(u => u.Id == id);

                // ✅ CONSTRUIR RESPOSTA DETALHADA
                var papeisDepois = usuarioAtualizado.UsuarioPapeis
                    .Where(up => up.Ativo)
                    .Select(up => new PapelComDetalhes
                    {
                        Id = up.Papel.Id,
                        Nome = up.Papel.Name!,
                        Descricao = up.Papel.Descricao,
                        Categoria = up.Papel.Categoria,
                        Nivel = up.Papel.Nivel,
                        DataAtribuicao = up.DataAtribuicao,
                        DataExpiracao = up.DataExpiracao,
                        AtribuidoPorId = up.AtribuidoPorId,
                        Permissoes = up.Papel.PapelPermissoes
                            .Where(pp => pp.Ativo && pp.Permissao.Ativo)
                            .Select(pp => new DTOs.Permissao.PermissaoResumo
                            {
                                Id = pp.Permissao.Id,
                                Nome = pp.Permissao.Nome,
                                Descricao = pp.Permissao.Descricao,
                                Categoria = pp.Permissao.Categoria
                            })
                            .ToList()
                    })
                    .OrderBy(p => p.Nome)
                    .ToList();

                var resposta = new RespostaGerenciamentoPapeis
                {
                    Sucesso = true,
                    Mensagem = $"Papéis {request.Operacao.ToLower()}s com sucesso",
                    Usuario = new UsuarioResumo
                    {
                        Id = usuario.Id,
                        Email = usuario.Email!,
                        Nome = usuario.Nome,
                        Sobrenome = usuario.Sobrenome,
                        NomeCompleto = usuario.NomeCompleto ?? $"{usuario.Nome} {usuario.Sobrenome}",
                        Ativo = usuario.Ativo,
                        DataCriacao = usuario.DataCriacao,
                        UltimoLogin = usuario.UltimoLogin,
                        Papeis = papeisDepois.Select(p => p.Nome).ToList(),
                        TotalPermissoes = papeisDepois.SelectMany(p => p.Permissoes).DistinctBy(p => p.Nome).Count()
                    },
                    PapeisAtuais = papeisDepois,
                    Estatisticas = new EstatisticasOperacao
                    {
                        TotalPapeisAntes = papeisAntes.Count,
                        TotalPapeisDepois = papeisDepois.Count,
                        TotalPermissoes = papeisDepois.SelectMany(p => p.Permissoes).DistinctBy(p => p.Nome).Count()
                    }
                };

                // ✅ REGISTRAR AUDITORIA DETALHADA
                await RegistrarAuditoria("Usuarios.GerenciarPapeis", "Usuarios", id.ToString(),
                    $"Papéis {request.Operacao.ToLower()}s para usuário: {usuario.Email}",
                    new {
                        PapeisAntes = papeisAntes,
                        Operacao = request.Operacao,
                        PapeisOperacao = request.Papeis
                    },
                    new {
                        PapeisDepois = papeisDepois.Select(p => new { p.Nome, p.Id, p.DataAtribuicao }),
                        Estatisticas = resposta.Estatisticas
                    });

                _logger.LogInformation("✅ Papéis gerenciados - ID: {Id}, Operação: {Operacao}, Antes: {Antes}, Depois: {Depois}",
                    id, request.Operacao, papeisAntes.Count, papeisDepois.Count);

                return Ok(resposta);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao gerenciar papéis do usuário ID: {Id}", id);
            return StatusCode(500, new RespostaErro
            {
                Erro = "ErroInterno",
                Mensagem = "Erro interno ao gerenciar papéis do usuário"
            });
        }
    }

    /// <summary>
    /// Lista todos os papéis disponíveis no sistema
    /// </summary>
    /// <returns>Lista de papéis disponíveis</returns>
    [HttpGet("papeis/disponiveis")]
    [ProducesResponseType(typeof(List<PapelDisponivel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListarPapeisDisponiveis()
    {
        try
        {
            // ✅ VERIFICAR PERMISSÃO
            if (!TemPermissao("Usuarios.GerenciarPapeis") && !TemPermissao("Usuarios.Visualizar"))
            {
                return Forbid("Permissão insuficiente para listar papéis");
            }

            _logger.LogInformation("📋 Listando papéis disponíveis - Solicitado por: {UsuarioLogadoId}", ObterUsuarioLogadoId());

            var papeis = await _roleManager.Roles
                .Where(r => r.Ativo)
                .Include(r => r.PapelPermissoes.Where(pp => pp.Ativo))
                    .ThenInclude(pp => pp.Permissao)
                .OrderBy(r => r.Categoria)
                .ThenBy(r => r.Nivel)
                .ThenBy(r => r.Name)
                .Select(r => new PapelDisponivel
                {
                    Id = r.Id,
                    Nome = r.Name!,
                    Descricao = r.Descricao,
                    Categoria = r.Categoria,
                    Nivel = r.Nivel,
                    TotalPermissoes = r.PapelPermissoes.Count(pp => pp.Ativo && pp.Permissao.Ativo),
                    Permissoes = r.PapelPermissoes
                        .Where(pp => pp.Ativo && pp.Permissao.Ativo)
                        .Select(pp => new DTOs.Permissao.PermissaoResumo
                        {
                            Id = pp.Permissao.Id,
                            Nome = pp.Permissao.Nome,
                            Descricao = pp.Permissao.Descricao,
                            Categoria = pp.Permissao.Categoria
                        })
                        .ToList()
                })
                .ToListAsync();

            _logger.LogInformation("✅ Papéis listados - Total: {Total}", papeis.Count);

            return Ok(papeis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao listar papéis disponíveis");
            return StatusCode(500, new RespostaErro
            {
                Erro = "ErroInterno",
                Mensagem = "Erro interno ao listar papéis disponíveis"
            });
        }
    }

    /// <summary>
    /// Busca avançada de usuários com filtros complexos
    /// </summary>
    /// <param name="request">Critérios de busca avançada</param>
    /// <returns>Resultados da busca</returns>
    [HttpPost("search")]
    [ProducesResponseType(typeof(RespostaBuscaAvancada), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> BuscarUsuarios([FromBody] SolicitacaoBuscaAvancada request)
    {
        try
        {
            // ✅ VERIFICAR PERMISSÃO
            if (!TemPermissao("Usuarios.Listar") && !TemPermissao("Usuarios.Buscar"))
            {
                return Forbid("Permissão insuficiente para buscar usuários");
            }

            var usuarioLogadoId = ObterUsuarioLogadoId();
            _logger.LogInformation("🔍 Busca avançada de usuários - Solicitado por: {UsuarioLogadoId}, Critérios: {@Criterios}",
                usuarioLogadoId, request);

            // ✅ VALIDAÇÃO AVANÇADA
            var validador = new BuscaAvancadaValidator();
            var resultadoValidacao = await validador.ValidateAsync(request);

            if (!resultadoValidacao.IsValid)
            {
                return BadRequest(new RespostaErro
                {
                    Erro = "CriteriosInvalidos",
                    Mensagem = "Critérios de busca são inválidos",
                    Detalhes = resultadoValidacao.Errors.Select(e => e.ErrorMessage).ToList()
                });
            }

            var inicio = DateTime.UtcNow;

            // ✅ CONSTRUIR QUERY DINÂMICA
            var query = _context.Users.AsQueryable();

            // ✅ APLICAR FILTROS AVANÇADOS
            query = await AplicarFiltrosAvancados(query, request);

            // ✅ INCLUIR RELACIONAMENTOS (otimizado)
            query = query.Include(u => u.UsuarioPapeis.Where(up => up.Ativo))
                         .ThenInclude(up => up.Papel)
                         .Include(u => u.UsuarioGrupos.Where(ug => ug.Ativo))
                         .ThenInclude(ug => ug.Grupo)
                         .AsSplitQuery();

            // ✅ CONTAR TOTAL (otimizado)
            var totalEncontrados = await query.CountAsync();

            // ✅ APLICAR ORDENAÇÃO PERSONALIZADA
            query = AplicarOrdenacaoAvancada(query, request.Ordenacao);

            // ✅ APLICAR PAGINAÇÃO
            var usuarios = await query
                .Skip((request.Pagina - 1) * request.ItensPorPagina)
                .Take(request.ItensPorPagina)
                .Select(u => new UsuarioBuscaResultado
                {
                    Id = u.Id,
                    Email = u.Email!,
                    Nome = u.Nome,
                    Sobrenome = u.Sobrenome,
                    NomeCompleto = u.NomeCompleto ?? $"{u.Nome} {u.Sobrenome}",
                    Telefone = u.PhoneNumber,
                    Ativo = u.Ativo,
                    DataCriacao = u.DataCriacao,
                    DataAtualizacao = u.DataAtualizacao,
                    UltimoLogin = u.UltimoLogin,
                    EmailConfirmado = u.EmailConfirmed,
                    TelefoneConfirmado = u.PhoneNumberConfirmed,
                    Papeis = u.UsuarioPapeis
                        .Where(up => up.Ativo && up.Papel.Ativo)
                        .Select(up => new PapelBusca
                        {
                            Nome = up.Papel.Name!,
                            Descricao = up.Papel.Descricao,
                            Categoria = up.Papel.Categoria,
                            DataAtribuicao = up.DataAtribuicao
                        })
                        .ToList(),
                    Grupos = u.UsuarioGrupos
                        .Where(ug => ug.Ativo && ug.Grupo.Ativo)
                        .Select(ug => new GrupoBusca
                        {
                            Nome = ug.Grupo.Nome,
                            Tipo = ug.Grupo.Tipo,
                            DataAdesao = ug.DataAdesao
                        })
                        .ToList(),
                    Estatisticas = new EstatisticasBusca
                    {
                        TotalPapeis = u.UsuarioPapeis.Count(up => up.Ativo),
                        TotalGrupos = u.UsuarioGrupos.Count(ug => ug.Ativo),
                        TotalPermissoes = u.UsuarioPapeis
                            .Where(up => up.Ativo && up.Papel.Ativo)
                            .SelectMany(up => up.Papel.PapelPermissoes)
                            .Where(pp => pp.Ativo)
                            .Count()
                    }
                })
                .ToListAsync();

            var tempoExecucao = DateTime.UtcNow - inicio;

            // ✅ CONSTRUIR ESTATÍSTICAS AGREGADAS
            var estatisticasAgregadas = await ConstruirEstatisticasAgregadas(query);

            var resposta = new RespostaBuscaAvancada
            {
                Resultados = usuarios,
                TotalEncontrados = totalEncontrados,
                TotalPaginas = (int)Math.Ceiling((double)totalEncontrados / request.ItensPorPagina),
                PaginaAtual = request.Pagina,
                ItensPorPagina = request.ItensPorPagina,
                TemProximaPagina = request.Pagina * request.ItensPorPagina < totalEncontrados,
                TemPaginaAnterior = request.Pagina > 1,
                CriteriosBusca = request,
                EstatisticasAgregadas = estatisticasAgregadas,
                TempoExecucao = tempoExecucao,
                ExecutadoEm = DateTime.UtcNow
            };

            // ✅ REGISTRAR AUDITORIA DA BUSCA
            await RegistrarAuditoria("Usuarios.Buscar", "Usuarios", null,
                $"Busca avançada executada: {totalEncontrados} resultados encontrados",
                dadosDepois: new {
                    TotalEncontrados = totalEncontrados,
                    TempoExecucao = tempoExecucao.TotalMilliseconds,
                    Criterios = request
                });

            _logger.LogInformation("✅ Busca avançada concluída - Encontrados: {Total}, Tempo: {Tempo}ms, Página: {Pagina}/{TotalPaginas}",
                totalEncontrados, tempoExecucao.TotalMilliseconds, request.Pagina, resposta.TotalPaginas);

            return Ok(resposta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro na busca avançada de usuários");
            return StatusCode(500, new RespostaErro
            {
                Erro = "ErroInterno",
                Mensagem = "Erro interno na busca de usuários"
            });
        }
    }

    /// <summary>
    /// Obtém sugestões de busca para autocompletar
    /// </summary>
    /// <param name="termo">Termo para busca de sugestões</param>
    /// <param name="tipo">Tipo de sugestão: email, nome, papel, grupo</param>
    /// <param name="limite">Limite de sugestões (máximo 20)</param>
    /// <returns>Lista de sugestões</returns>
    [HttpGet("search/sugestoes")]
    [ProducesResponseType(typeof(List<SugestaoBusca>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ObterSugestoesBusca([FromQuery] string termo, [FromQuery] string tipo = "all", [FromQuery] int limite = 10)
    {
        try
        {
            // ✅ VERIFICAR PERMISSÃO
            if (!TemPermissao("Usuarios.Listar"))
            {
                return Forbid("Permissão insuficiente para obter sugestões");
            }

            if (string.IsNullOrWhiteSpace(termo) || termo.Length < 2)
            {
                return BadRequest(new RespostaErro
                {
                    Erro = "TermoInvalido",
                    Mensagem = "Termo deve ter pelo menos 2 caracteres"
                });
            }

            limite = Math.Min(limite, 20); // Máximo 20 sugestões
            var sugestoes = new List<SugestaoBusca>();

            var termoLower = termo.ToLower();

            switch (tipo.ToLower())
            {
                case "email":
                    sugestoes.AddRange(await ObterSugestoesEmail(termoLower, limite));
                    break;

                case "nome":
                    sugestoes.AddRange(await ObterSugestoesNome(termoLower, limite));
                    break;

                case "papel":
                    sugestoes.AddRange(await ObterSugestoesPapel(termoLower, limite));
                    break;

                case "grupo":
                    sugestoes.AddRange(await ObterSugestoesGrupo(termoLower, limite));
                    break;

                case "all":
                default:
                    var limitePorTipo = Math.Max(1, limite / 4);
                    sugestoes.AddRange(await ObterSugestoesEmail(termoLower, limitePorTipo));
                    sugestoes.AddRange(await ObterSugestoesNome(termoLower, limitePorTipo));
                    sugestoes.AddRange(await ObterSugestoesPapel(termoLower, limitePorTipo));
                    sugestoes.AddRange(await ObterSugestoesGrupo(termoLower, limitePorTipo));
                    break;
            }

            // ✅ ORDENAR POR RELEVÂNCIA E LIMITAR
            var sugestoesOrdenadas = sugestoes
                .OrderBy(s => s.Valor.Length) // Strings menores primeiro (mais exatas)
                .ThenBy(s => s.Valor)
                .Take(limite)
                .ToList();

            _logger.LogInformation("🔍 Sugestões obtidas - Termo: {Termo}, Tipo: {Tipo}, Total: {Total}",
                termo, tipo, sugestoesOrdenadas.Count);

            return Ok(sugestoesOrdenadas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao obter sugestões de busca");
            return StatusCode(500, new RespostaErro
            {
                Erro = "ErroInterno",
                Mensagem = "Erro interno ao obter sugestões"
            });
        }
    }

    /// <summary>
    /// Executa operações em lote (bulk) com usuários
    /// </summary>
    /// <param name="request">Dados para operação em lote</param>
    /// <returns>Resultado detalhado das operações</returns>
    [HttpPost("bulk")]
    [ProducesResponseType(typeof(RespostaOperacaoLote), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaOperacaoLote), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExecutarOperacaoLote([FromBody] SolicitacaoOperacaoLote request)
    {
        try
        {
            var usuarioLogadoId = ObterUsuarioLogadoId();

            // ✅ VERIFICAR PERMISSÃO GERAL PARA OPERAÇÕES EM LOTE
            if (!TemPermissao("Usuarios.OperacoesLote"))
            {
                return Forbid("Permissão insuficiente para operações em lote");
            }

            _logger.LogInformation("📦 Operação em lote iniciada - Tipo: {TipoOperacao}, Usuário: {UsuarioLogadoId}",
                request.TipoOperacao, usuarioLogadoId);

            // ✅ VALIDAÇÃO CORRIGIDA COM PARÂMETROS CORRETOS
            var validador = new OperacaoLoteValidator(_userManager, _roleManager);
            var resultadoValidacao = await validador.ValidateAsync(request);

            if (!resultadoValidacao.IsValid)
            {
                return BadRequest(new RespostaErro
                {
                    Erro = "DadosInvalidos",
                    Mensagem = "Dados da operação em lote são inválidos",
                    Detalhes = resultadoValidacao.Errors.Select(e => e.ErrorMessage).ToList()
                });
            }

            // ✅ VERIFICAR PERMISSÕES ESPECÍFICAS POR OPERAÇÃO
            var permissaoEspecifica = VerificarPermissaoEspecificaLote(request.TipoOperacao);
            if (!string.IsNullOrEmpty(permissaoEspecifica) && !TemPermissao(permissaoEspecifica))
            {
                return Forbid($"Permissão insuficiente para operação: {request.TipoOperacao}");
            }

            // ✅ DETERMINAR SE DEVE EXECUTAR ASSÍNCRONO
            var deveSerAssincrono = request.ExecutarAssincrono || DeveSerExecutadoAssincrono(request);

            if (deveSerAssincrono)
            {
                // ✅ EXECUÇÃO ASSÍNCRONA (para operações grandes)
                var jobId = Guid.NewGuid().ToString();

                // Iniciar tarefa em background (implementar com IHostedService ou Hangfire posteriormente)
                _ = Task.Run(async () =>
                {
                    await ExecutarOperacaoLoteAsync(request, usuarioLogadoId, jobId);
                });

                var respostaAssincrona = new RespostaOperacaoLote
                {
                    Sucesso = true,
                    TipoOperacao = request.TipoOperacao,
                    JobId = jobId,
                    Mensagem = "Operação iniciada em background",
                    IniciadoEm = DateTime.UtcNow
                };

                return Accepted(respostaAssincrona);
            }
            else
            {
                // ✅ EXECUÇÃO SÍNCRONA (para operações pequenas/rápidas)
                var resultado = await ExecutarOperacaoLoteAsync(request, usuarioLogadoId);
                return Ok(resultado);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro na operação em lote");
            return StatusCode(500, new RespostaErro
            {
                Erro = "ErroInterno",
                Mensagem = "Erro interno na operação em lote"
            });
        }
    }

    /// <summary>
    /// Obtém status de uma operação em lote em execução
    /// </summary>
    /// <param name="jobId">ID do job da operação</param>
    /// <returns>Status da operação</returns>
    [HttpGet("bulk/{jobId}/status")]
    [ProducesResponseType(typeof(StatusOperacaoLote), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    public IActionResult ObterStatusOperacaoLote(string jobId)
    {
        try
        {
            // ✅ IMPLEMENTAR: Buscar status do job (Redis, banco, memória cache)
            // Por enquanto, resposta mock
            var status = new StatusOperacaoLote
            {
                JobId = jobId,
                Status = "Executando",
                Progresso = 50,
                IniciadoEm = DateTime.UtcNow.AddMinutes(-5),
                TotalItens = 100,
                ItensProcessados = 50,
                ItensSucesso = 48,
                ItensErro = 2,
                Mensagem = "Processando usuários..."
            };

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao obter status da operação em lote");
            return StatusCode(500, new RespostaErro
            {
                Erro = "ErroInterno",
                Mensagem = "Erro interno ao obter status"
            });
        }
    }

    /// <summary>
    /// Cancela uma operação em lote em execução
    /// </summary>
    /// <param name="jobId">ID do job da operação</param>
    /// <returns>Confirmação do cancelamento</returns>
    [HttpDelete("bulk/{jobId}/cancelar")]
    [ProducesResponseType(typeof(RespostaSucesso), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public IActionResult CancelarOperacaoLote(string jobId)
    {
        try
        {
            if (!TemPermissao("Usuarios.OperacaoLote"))
            {
                return Forbid("Permissão insuficiente para cancelar operações em lote");
            }

            // ✅ Em uma implementação real, você cancelaria o job
            _logger.LogInformation("🛑 Cancelando operação em lote: {JobId}", jobId);

            return Ok(new RespostaSucesso
            {
                Sucesso = true,
                Mensagem = $"Operação {jobId} cancelada com sucesso"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao cancelar operação em lote: {JobId}", jobId);
            return StatusCode(500, new RespostaErro
            {
                Erro = "ErroInterno",
                Mensagem = "Erro interno ao cancelar operação"
            });
        }
    }
    
    /// <summary>
    /// Obtém perfil do usuário logado
    /// </summary>
    /// <returns>Perfil completo do usuário</returns>
    [HttpGet("perfil")]
    [ProducesResponseType(typeof(PerfilUsuario), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ObterMeuPerfil()
    {
        try
        {
            var usuarioLogadoId = ObterUsuarioLogadoId();
            
            var usuario = await _context.Users
                .Include(u => u.UsuarioPapeis.Where(up => up.Ativo))
                    .ThenInclude(up => up.Papel)
                .Include(u => u.UsuarioGrupos.Where(ug => ug.Ativo))
                    .ThenInclude(ug => ug.Grupo)
                .FirstOrDefaultAsync(u => u.Id == usuarioLogadoId);

            if (usuario == null)
            {
                return NotFound(new RespostaErro { Erro = "UsuarioNaoEncontrado", Mensagem = "Usuário não encontrado" });
            }

            var perfil = new PerfilUsuario
            {
                Id = usuario.Id,
                Email = usuario.Email!,
                Nome = usuario.Nome,
                Sobrenome = usuario.Sobrenome,
                NomeCompleto = usuario.NomeCompleto ?? $"{usuario.Nome} {usuario.Sobrenome}",
                Telefone = usuario.PhoneNumber,
                CaminhoFotoPerfil = usuario.CaminhoFotoPerfil,
                UrlFotoPerfil = !string.IsNullOrEmpty(usuario.CaminhoFotoPerfil) 
                    ? _arquivoService.GerarUrlSegura(usuario.CaminhoFotoPerfil) 
                    : null,
                Profissao = usuario.Profissao,
                Departamento = usuario.Departamento,
                Bio = usuario.Bio,
                DataNascimento = usuario.DataNascimento,
                EnderecoCompleto = usuario.EnderecoCompleto,
                Cidade = usuario.Cidade,
                Estado = usuario.Estado,
                Cep = usuario.Cep,
                TelefoneAlternativo = usuario.TelefoneAlternativo,
                PreferenciaIdioma = usuario.PreferenciaIdioma ?? "pt-BR",
                PreferenciaTimezone = usuario.PreferenciaTimezone ?? "America/Sao_Paulo",
                Privacidade = new ConfiguracaoPrivacidade
                {
                    ExibirEmail = usuario.ExibirEmail,
                    ExibirTelefone = usuario.ExibirTelefone,
                    ExibirDataNascimento = usuario.ExibirDataNascimento,
                    ExibirEndereco = usuario.ExibirEndereco,
                    PerfilPublico = usuario.PerfilPublico
                },
                Notificacoes = new ConfiguracaoNotificacao
                {
                    NotificacaoEmail = usuario.NotificacaoEmail,
                    NotificacaoSms = usuario.NotificacaoSms,
                    NotificacaoPush = usuario.NotificacaoPush
                },
                DataCriacao = usuario.DataCriacao,
                DataAtualizacao = usuario.DataAtualizacao,
                UltimoLogin = usuario.UltimoLogin
            };

            return Ok(perfil);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao obter perfil do usuário: {UsuarioId}", ObterUsuarioLogadoId());
            return StatusCode(500, new RespostaErro { Erro = "ErroInterno", Mensagem = "Erro interno ao obter perfil" });
        }
    }

    /// <summary>
    /// Atualiza perfil do usuário logado
    /// </summary>
    /// <param name="request">Dados para atualização do perfil</param>
    /// <returns>Perfil atualizado</returns>
    [HttpPut("perfil")]
    [ProducesResponseType(typeof(PerfilUsuario), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AtualizarMeuPerfil([FromBody] AtualizarPerfilRequest request)
    {
        try
        {
            var usuarioLogadoId = ObterUsuarioLogadoId();
            
            // Validar dados
            if (!ModelState.IsValid)
            {
                return BadRequest(new RespostaErro 
                { 
                    Erro = "DadosInvalidos", 
                    Mensagem = "Dados fornecidos são inválidos",
                    Detalhes = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            var strategy = _context.Database.CreateExecutionStrategy();
            var resultado = await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var usuario = await _userManager.FindByIdAsync(usuarioLogadoId.ToString());
                    if (usuario == null)
                    {
                        throw new InvalidOperationException("Usuário não encontrado");
                    }

                    // Capturar dados antes da atualização
                    var dadosAntes = JsonSerializer.Serialize(new
                    {
                        usuario.Nome,
                        usuario.Sobrenome,
                        usuario.PhoneNumber,
                        usuario.Profissao,
                        usuario.Departamento,
                        usuario.Bio,
                        usuario.DataNascimento,
                        usuario.EnderecoCompleto,
                        usuario.Cidade,
                        usuario.Estado,
                        usuario.Cep,
                        usuario.TelefoneAlternativo,
                        usuario.PreferenciaIdioma,
                        usuario.PreferenciaTimezone
                    });

                    // Atualizar campos do perfil
                    if (!string.IsNullOrEmpty(request.Nome))
                        usuario.Nome = request.Nome;
                    
                    if (!string.IsNullOrEmpty(request.Sobrenome))
                        usuario.Sobrenome = request.Sobrenome;
                    
                    if (!string.IsNullOrEmpty(request.Telefone))
                        usuario.PhoneNumber = request.Telefone;
                    
                    if (!string.IsNullOrEmpty(request.Profissao))
                        usuario.Profissao = request.Profissao;
                    
                    if (!string.IsNullOrEmpty(request.Departamento))
                        usuario.Departamento = request.Departamento;
                    
                    if (!string.IsNullOrEmpty(request.Bio))
                        usuario.Bio = request.Bio;
                    
                    if (request.DataNascimento.HasValue)
                        usuario.DataNascimento = request.DataNascimento;
                    
                    if (!string.IsNullOrEmpty(request.EnderecoCompleto))
                        usuario.EnderecoCompleto = request.EnderecoCompleto;
                    
                    if (!string.IsNullOrEmpty(request.Cidade))
                        usuario.Cidade = request.Cidade;
                    
                    if (!string.IsNullOrEmpty(request.Estado))
                        usuario.Estado = request.Estado;
                    
                    if (!string.IsNullOrEmpty(request.Cep))
                        usuario.Cep = request.Cep;
                    
                    if (!string.IsNullOrEmpty(request.TelefoneAlternativo))
                        usuario.TelefoneAlternativo = request.TelefoneAlternativo;
                    
                    if (!string.IsNullOrEmpty(request.PreferenciaIdioma))
                        usuario.PreferenciaIdioma = request.PreferenciaIdioma;
                    
                    if (!string.IsNullOrEmpty(request.PreferenciaTimezone))
                        usuario.PreferenciaTimezone = request.PreferenciaTimezone;

                    // Atualizar nome completo
                    usuario.NomeCompleto = $"{usuario.Nome} {usuario.Sobrenome}";
                    usuario.DataAtualizacao = DateTime.UtcNow;

                    // Salvar alterações
                    _context.Users.Update(usuario);
                    await _context.SaveChangesAsync();

                    // Registrar auditoria
                    var dadosDepois = JsonSerializer.Serialize(new
                    {
                        usuario.Nome,
                        usuario.Sobrenome,
                        usuario.PhoneNumber,
                        usuario.Profissao,
                        usuario.Departamento,
                        usuario.Bio,
                        usuario.DataNascimento,
                        usuario.EnderecoCompleto,
                        usuario.Cidade,
                        usuario.Estado,
                        usuario.Cep,
                        usuario.TelefoneAlternativo,
                        usuario.PreferenciaIdioma,
                        usuario.PreferenciaTimezone
                    });

                    var registroAuditoria = new RegistroAuditoria
                    {
                        UsuarioId = usuarioLogadoId,
                        Acao = "Perfil.Atualizar",
                        Recurso = "Usuario",
                        RecursoId = usuario.Id.ToString(),
                        DadosAntes = dadosAntes,
                        DadosDepois = dadosDepois,
                        DataHora = DateTime.UtcNow,
                        EnderecoIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
                        UserAgent = Request.Headers.UserAgent.ToString(),
                        Observacoes = "Perfil atualizado pelo próprio usuário"
                    };

                    _context.RegistrosAuditoria.Add(registroAuditoria);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    return usuario;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });

            _logger.LogInformation("✅ Perfil atualizado com sucesso - Usuário: {Id}", usuarioLogadoId);

            // Retornar perfil atualizado
            return await ObterMeuPerfil();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao atualizar perfil do usuário: {UsuarioId}", ObterUsuarioLogadoId());
            return StatusCode(500, new RespostaErro { Erro = "ErroInterno", Mensagem = "Erro interno ao atualizar perfil" });
        }
    }

    /// <summary>
    /// Atualiza configurações de privacidade do usuário
    /// </summary>
    /// <param name="request">Configurações de privacidade</param>
    /// <returns>Confirmação da atualização</returns>
    [HttpPut("perfil/privacidade")]
    [ProducesResponseType(typeof(RespostaSucesso), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AtualizarConfiguracoesPrivacidade([FromBody] ConfiguracaoPrivacidade request)
    {
        try
        {
            var usuarioLogadoId = ObterUsuarioLogadoId();
            
            var usuario = await _userManager.FindByIdAsync(usuarioLogadoId.ToString());
            if (usuario == null)
            {
                return NotFound(new RespostaErro { Erro = "UsuarioNaoEncontrado", Mensagem = "Usuário não encontrado" });
            }

            // Capturar configurações antes
            var configAntes = new
            {
                usuario.ExibirEmail,
                usuario.ExibirTelefone,
                usuario.ExibirDataNascimento,
                usuario.ExibirEndereco,
                usuario.PerfilPublico
            };

            // Atualizar configurações
            usuario.ExibirEmail = request.ExibirEmail;
            usuario.ExibirTelefone = request.ExibirTelefone;
            usuario.ExibirDataNascimento = request.ExibirDataNascimento;
            usuario.ExibirEndereco = request.ExibirEndereco;
            usuario.PerfilPublico = request.PerfilPublico;
            usuario.DataAtualizacao = DateTime.UtcNow;

            _context.Users.Update(usuario);
            await _context.SaveChangesAsync();

            // Registrar auditoria
            await RegistrarAuditoria("Perfil.ConfiguracaoPrivacidade", "Usuario", usuario.Id.ToString(),
                "Configurações de privacidade atualizadas",
                configAntes,
                new
                {
                    usuario.ExibirEmail,
                    usuario.ExibirTelefone,
                    usuario.ExibirDataNascimento,
                    usuario.ExibirEndereco,
                    usuario.PerfilPublico
                });

            _logger.LogInformation("✅ Configurações de privacidade atualizadas - Usuário: {Id}", usuarioLogadoId);

            return Ok(new RespostaSucesso
            {
                Sucesso = true,
                Mensagem = "Configurações de privacidade atualizadas com sucesso"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao atualizar configurações de privacidade: {UsuarioId}", ObterUsuarioLogadoId());
            return StatusCode(500, new RespostaErro { Erro = "ErroInterno", Mensagem = "Erro interno" });
        }
    }

    /// <summary>
    /// Atualiza configurações de notificação do usuário
    /// </summary>
    /// <param name="request">Configurações de notificação</param>
    /// <returns>Confirmação da atualização</returns>
    [HttpPut("perfil/notificacoes")]
    [ProducesResponseType(typeof(RespostaSucesso), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AtualizarConfiguracoesNotificacao([FromBody] ConfiguracaoNotificacao request)
    {
        try
        {
            var usuarioLogadoId = ObterUsuarioLogadoId();
            
            var usuario = await _userManager.FindByIdAsync(usuarioLogadoId.ToString());
            if (usuario == null)
            {
                return NotFound(new RespostaErro { Erro = "UsuarioNaoEncontrado", Mensagem = "Usuário não encontrado" });
            }

            // Capturar configurações antes
            var configAntes = new
            {
                usuario.NotificacaoEmail,
                usuario.NotificacaoSms,
                usuario.NotificacaoPush
            };

            // Atualizar configurações
            usuario.NotificacaoEmail = request.NotificacaoEmail;
            usuario.NotificacaoSms = request.NotificacaoSms;
            usuario.NotificacaoPush = request.NotificacaoPush;
            usuario.DataAtualizacao = DateTime.UtcNow;

            _context.Users.Update(usuario);
            await _context.SaveChangesAsync();

            // Registrar auditoria
            await RegistrarAuditoria("Perfil.ConfiguracaoNotificacao", "Usuario", usuario.Id.ToString(),
                "Configurações de notificação atualizadas",
                configAntes,
                new
                {
                    usuario.NotificacaoEmail,
                    usuario.NotificacaoSms,
                    usuario.NotificacaoPush
                });

            _logger.LogInformation("✅ Configurações de notificação atualizadas - Usuário: {Id}", usuarioLogadoId);

            return Ok(new RespostaSucesso
            {
                Sucesso = true,
                Mensagem = "Configurações de notificação atualizadas com sucesso"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao atualizar configurações de notificação: {UsuarioId}", ObterUsuarioLogadoId());
            return StatusCode(500, new RespostaErro { Erro = "ErroInterno", Mensagem = "Erro interno" });
        }
    }

    /// <summary>
    /// Faz upload da foto de perfil do usuário
    /// </summary>
    /// <param name="arquivo">Arquivo de imagem</param>
    /// <returns>URL da foto de perfil</returns>
    [HttpPost("perfil/foto")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadFotoPerfil(IFormFile arquivo)
    {
        try
        {
            var usuarioLogadoId = ObterUsuarioLogadoId();
            
            if (!_arquivoService.ValidarImagemPerfil(arquivo))
            {
                return BadRequest(new RespostaErro 
                { 
                    Erro = "ArquivoInvalido", 
                    Mensagem = "Arquivo de imagem inválido. Aceitos: JPG, PNG, WEBP até 5MB" 
                });
            }

            var usuario = await _userManager.FindByIdAsync(usuarioLogadoId.ToString());
            if (usuario == null)
            {
                return NotFound(new RespostaErro { Erro = "UsuarioNaoEncontrado", Mensagem = "Usuário não encontrado" });
            }

            // Excluir foto anterior se existir
            if (!string.IsNullOrEmpty(usuario.CaminhoFotoPerfil))
            {
                await _arquivoService.ExcluirImagemPerfilAsync(usuario.CaminhoFotoPerfil);
            }

            // Salvar nova foto
            var caminhoArquivo = await _arquivoService.SalvarImagemPerfilAsync(arquivo, usuarioLogadoId);
            
            // Atualizar usuário
            usuario.CaminhoFotoPerfil = caminhoArquivo;
            usuario.DataAtualizacao = DateTime.UtcNow;

            _context.Users.Update(usuario);
            await _context.SaveChangesAsync();

            // Registrar auditoria
            await RegistrarAuditoria("Perfil.FotoUpload", "Usuario", usuario.Id.ToString(),
                "Foto de perfil atualizada",
                new { FotoAnterior = usuario.CaminhoFotoPerfil },
                new { NovaFoto = caminhoArquivo });

            var urlSegura = _arquivoService.GerarUrlSegura(caminhoArquivo);

            _logger.LogInformation("✅ Foto de perfil atualizada - Usuário: {Id}, Arquivo: {Caminho}", 
                usuarioLogadoId, caminhoArquivo);

            return Ok(new
            {
                sucesso = true,
                mensagem = "Foto de perfil atualizada com sucesso",
                urlFoto = urlSegura,
                caminhoArquivo = caminhoArquivo
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao fazer upload da foto de perfil: {UsuarioId}", ObterUsuarioLogadoId());
            return StatusCode(500, new RespostaErro { Erro = "ErroInterno", Mensagem = "Erro interno no upload" });
        }
    }

    /// <summary>
    /// Remove a foto de perfil do usuário
    /// </summary>
    /// <returns>Confirmação da remoção</returns>
    [HttpDelete("perfil/foto")]
    [ProducesResponseType(typeof(RespostaSucesso), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoverFotoPerfil()
    {
        try
        {
            var usuarioLogadoId = ObterUsuarioLogadoId();
            
            var usuario = await _userManager.FindByIdAsync(usuarioLogadoId.ToString());
            if (usuario == null)
            {
                return NotFound(new RespostaErro { Erro = "UsuarioNaoEncontrado", Mensagem = "Usuário não encontrado" });
            }

            if (string.IsNullOrEmpty(usuario.CaminhoFotoPerfil))
            {
                return BadRequest(new RespostaErro { Erro = "SemFoto", Mensagem = "Usuário não possui foto de perfil" });
            }

            var caminhoAnterior = usuario.CaminhoFotoPerfil;

            // Excluir arquivo
            await _arquivoService.ExcluirImagemPerfilAsync(usuario.CaminhoFotoPerfil);

            // Atualizar usuário
            usuario.CaminhoFotoPerfil = null;
            usuario.DataAtualizacao = DateTime.UtcNow;

            _context.Users.Update(usuario);
            await _context.SaveChangesAsync();

            // Registrar auditoria
            await RegistrarAuditoria("Perfil.FotoRemover", "Usuario", usuario.Id.ToString(),
                "Foto de perfil removida",
                new { FotoRemovida = caminhoAnterior },
                new { FotoAtual = (string?)null });

            _logger.LogInformation("✅ Foto de perfil removida - Usuário: {Id}, Arquivo: {Caminho}", 
                usuarioLogadoId, caminhoAnterior);

            return Ok(new RespostaSucesso
            {
                Sucesso = true,
                Mensagem = "Foto de perfil removida com sucesso"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao remover foto de perfil: {UsuarioId}", ObterUsuarioLogadoId());
            return StatusCode(500, new RespostaErro { Erro = "ErroInterno", Mensagem = "Erro interno" });
        }
    }

    /// <summary>
    /// Obtém foto de perfil de forma segura
    /// </summary>
    /// <param name="caminhoArquivo">Caminho do arquivo</param>
    /// <param name="t">Timestamp</param>
    /// <param name="h">Hash de segurança</param>
    /// <returns>Arquivo de imagem</returns>
    [HttpGet("perfil/imagem/{caminhoArquivo}")]
    [AllowAnonymous] // Permite acesso sem autenticação para imagens públicas
    public async Task<IActionResult> ObterImagemPerfil(string caminhoArquivo, [FromQuery] long t, [FromQuery] string h)
    {
        try
        {
            // Validar token de segurança (implementação básica)
            var agora = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var diferenca = Math.Abs(agora - t);
            
            // Token válido por 1 hora
            if (diferenca > 3600)
            {
                return BadRequest(new RespostaErro { Erro = "TokenExpirado", Mensagem = "Token de acesso expirado" });
            }

            var bytesImagem = await _arquivoService.ObterImagemPerfilAsync(caminhoArquivo);
            
            return File(bytesImagem, "image/jpeg");
        }
        catch (FileNotFoundException)
        {
            return NotFound(new RespostaErro { Erro = "ImagemNaoEncontrada", Mensagem = "Imagem não encontrada" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao obter imagem de perfil: {Caminho}", caminhoArquivo);
            return StatusCode(500, new RespostaErro { Erro = "ErroInterno", Mensagem = "Erro interno" });
        }
    }

    /// <summary>
    /// Lista aplicações de um usuário específico com filtros
    /// </summary>
    /// <param name="id">ID do usuário</param>
    /// <param name="filtros">Filtros para aplicações</param>
    /// <returns>Lista paginada de aplicações do usuário</returns>
    [HttpGet("{id:int}/aplicacoes")]
    [ProducesResponseType(typeof(RespostaPaginada<AplicacaoUsuario>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListarAplicacoesUsuario(int id, [FromQuery] FiltrosAplicacaoUsuario filtros)
    {
        try
        {
            if (!TemPermissao("Usuarios.Visualizar") && !EhProprioUsuario(id))
            {
                return StatusCode(403, new RespostaErro 
                { 
                    Erro = "AcessoNegado", 
                    Mensagem = "Usuário não tem permissão para visualizar aplicações deste usuário" 
                });
            }

            var usuario = await _userManager.FindByIdAsync(id.ToString());
            if (usuario == null)
            {
                return NotFound(new RespostaErro 
                { 
                    Erro = "UsuarioNaoEncontrado", 
                    Mensagem = "Usuário não encontrado" 
                });
            }

            var query = _context.UsuariosAplicacao
                .Include(ua => ua.Aplicacao)
                    .ThenInclude(a => a.TipoAplicacao)
                .Include(ua => ua.Aplicacao)
                    .ThenInclude(a => a.StatusAplicacao)
                .Include(ua => ua.AprovadoPor)
                .Where(ua => ua.UsuarioId == id)
                .AsQueryable();

            // Aplicar filtros
            query = AplicarFiltrosAplicacaoUsuario(query, filtros);

            // Aplicar ordenação
            query = AplicarOrdenacaoAplicacaoUsuario(query, filtros.OrdenarPor, filtros.DirecaoOrdenacao);

            // Paginação
            var totalItens = await query.CountAsync();
            var aplicacoesLista = await query
                .AsNoTracking()
                .Skip((filtros.Pagina - 1) * filtros.ItensPorPagina)
                .Take(filtros.ItensPorPagina)
                .ToListAsync();

            var aplicacoes = aplicacoesLista
                .Select(ua => ConstruirAplicacaoUsuario(ua))
                .ToList();

            var resposta = new RespostaPaginada<AplicacaoUsuario>
            {
                Dados = aplicacoes,
                TotalItens = totalItens,
                PaginaAtual = filtros.Pagina,
                ItensPorPagina = filtros.ItensPorPagina,
                TemPaginaAnterior = filtros.Pagina > 1
            };

            await RegistrarAuditoria("Visualizar", "UsuarioAplicacoes", id.ToString(), 
                $"Listagem de aplicações do usuário '{usuario.Email}'");

            return Ok(resposta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao listar aplicações do usuário {Id}", id);
            return StatusCode(500, new RespostaErro 
            { 
                Erro = "ErroInterno", 
                Mensagem = "Erro interno do servidor" 
            });
        }
    }

    /// <summary>
    /// Gerencia aplicações de um usuário (adicionar, remover, aprovar, etc.)
    /// </summary>
    /// <param name="id">ID do usuário</param>
    /// <param name="request">Dados para gerenciamento</param>
    /// <returns>Resultado da operação</returns>
    [HttpPost("{id:int}/aplicacoes")]
    [ProducesResponseType(typeof(RespostaGerenciamentoAplicacoes), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GerenciarAplicacoesUsuario(int id, [FromBody] GerenciarAplicacoesRequest request)
    {
        try
        {
            if (!TemPermissao("Usuarios.GerenciarAplicacoes"))
            {
                return StatusCode(403, new RespostaErro 
                { 
                    Erro = "AcessoNegado", 
                    Mensagem = "Usuário não tem permissão para gerenciar aplicações de usuários" 
                });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var usuario = await _userManager.FindByIdAsync(id.ToString());
            if (usuario == null)
            {
                return NotFound(new RespostaErro 
                { 
                    Erro = "UsuarioNaoEncontrado", 
                    Mensagem = "Usuário não encontrado" 
                });
            }

            var usuarioLogadoId = ObterUsuarioLogadoId();
            var resultadoOperacao = await ExecutarOperacaoAplicacoes(usuario, request, usuarioLogadoId);

            if (!resultadoOperacao.Sucesso)
            {
                return BadRequest(new RespostaErro 
                { 
                    Erro = "ErroOperacao", 
                    Mensagem = resultadoOperacao.Mensagem 
                });
            }

            // Obter aplicações atuais do usuário
            var aplicacoesAtuais = await ObterAplicacoesUsuarioAsync(id);

            var resposta = new RespostaGerenciamentoAplicacoes
            {
                Sucesso = true,
                Mensagem = resultadoOperacao.Mensagem,
                TipoOperacao = request.Operacao,
                Usuario = new UsuarioResumo
                {
                    Id = usuario.Id,
                    Email = usuario.Email!,
                    Nome = usuario.Nome,
                    Sobrenome = usuario.Sobrenome,
                    NomeCompleto = usuario.NomeCompleto ?? $"{usuario.Nome} {usuario.Sobrenome}",
                    Ativo = usuario.Ativo
                },
                AplicacoesAtuais = aplicacoesAtuais,
                Estatisticas = new EstatisticasOperacaoAplicacoes
                {
                    TotalAplicacoesAntes = aplicacoesAtuais?.Count ?? 0,
                    TotalAplicacoesDepois = aplicacoesAtuais?.Count ?? 0, // cálculo simplificado
                    AplicacoesAdicionadas = request.Operacao?.ToLower() == "adicionar" ? (request.AplicacoesIds?.Count ?? 0) : 0,
                    AplicacoesRemovidas = request.Operacao?.ToLower() == "remover" ? (request.AplicacoesIds?.Count ?? 0) : 0,
                    AplicacoesAprovadas = request.AprovarAutomaticamente ? (request.AplicacoesIds?.Count ?? 0) : 0,
                    AplicacoesRejeitadas = 0,
                    TotalPermissoes = 0,
                    NomesAplicacoesAdicionadas = new(),
                    NomesAplicacoesRemovidas = new()
                },
                Alertas = resultadoOperacao.Alertas,
                Erros = resultadoOperacao.Erros
            };

            await RegistrarAuditoria("GerenciarAplicacoes", "Usuario", id.ToString(), 
                $"Operação '{request.Operacao}' em aplicações do usuário '{usuario.Email}'", 
                null, request);

            // Enviar notificação se solicitado
            if (request.NotificarUsuario && !string.IsNullOrEmpty(usuario.Email))
            {
                await EnviarNotificacaoAplicacoes(usuario, request.Operacao ?? string.Empty, resultadoOperacao);
            }

            _logger.LogInformation("✅ Aplicações gerenciadas - Usuário: {Id}, Operação: {Operacao}, Admin: {AdminId}", 
                id, request.Operacao, usuarioLogadoId);

            return Ok(resposta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao gerenciar aplicações do usuário {Id}", id);
            return StatusCode(500, new RespostaErro 
            { 
                Erro = "ErroInterno", 
                Mensagem = "Erro interno do servidor" 
            });
        }
    }

    /// <summary>
    /// Aprova ou rejeita solicitação de acesso de usuário a uma aplicação
    /// </summary>
    /// <param name="id">ID do usuário</param>
    /// <param name="aplicacaoId">ID da aplicação</param>
    /// <param name="request">Dados da aprovação</param>
    /// <returns>Resultado da aprovação</returns>
    [HttpPost("{id:int}/aplicacoes/{aplicacaoId:int}/aprovar")]
    [ProducesResponseType(typeof(RespostaAprovacaoAcesso), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AprovarAcessoAplicacao(int id, int aplicacaoId, [FromBody] AprovarAcessoAplicacaoRequest request)
    {
        try
        {
            if (!TemPermissao("Usuarios.AprovarAplicacoes"))
            {
                return StatusCode(403, new RespostaErro 
                { 
                    Erro = "AcessoNegado", 
                    Mensagem = "Usuário não tem permissão para aprovar acesso a aplicações" 
                });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var usuario = await _userManager.FindByIdAsync(id.ToString());
            if (usuario == null)
            {
                return NotFound(new RespostaErro 
                { 
                    Erro = "UsuarioNaoEncontrado", 
                    Mensagem = "Usuário não encontrado" 
                });
            }

            var usuarioAplicacao = await _context.UsuariosAplicacao
                .Include(ua => ua.Aplicacao)
                    .ThenInclude(a => a.TipoAplicacao)
                .Include(ua => ua.Aplicacao)
                    .ThenInclude(a => a.StatusAplicacao)
                .FirstOrDefaultAsync(ua => ua.UsuarioId == id && ua.AplicacaoId == aplicacaoId);

            if (usuarioAplicacao == null)
            {
                return NotFound(new RespostaErro 
                { 
                    Erro = "SolicitacaoNaoEncontrada", 
                    Mensagem = "Solicitação de acesso não encontrada" 
                });
            }

            var usuarioLogadoId = ObterUsuarioLogadoId();
            var resultadoAprovacao = await ProcessarAprovacaoAcesso(usuarioAplicacao, request, usuarioLogadoId);

            if (!resultadoAprovacao.Sucesso)
            {
                return BadRequest(new RespostaErro 
                { 
                    Erro = "ErroAprovacao", 
                    Mensagem = resultadoAprovacao.Mensagem 
                });
            }

            var aplicacaoUsuario = await ObterAplicacaoUsuarioAsync(id, aplicacaoId);

            var resposta = new RespostaAprovacaoAcesso
            {
                Sucesso = true,
                Mensagem = resultadoAprovacao.Mensagem,
                DecisaoTomada = request.Decisao,
                Usuario = new UsuarioResumo
                {
                    Id = usuario.Id,
                    Email = usuario.Email!,
                    Nome = usuario.Nome,
                    Sobrenome = usuario.Sobrenome,
                    NomeCompleto = usuario.NomeCompleto ?? $"{usuario.Nome} {usuario.Sobrenome}",
                    Ativo = usuario.Ativo
                },
                AplicacaoAfetada = aplicacaoUsuario!,
                DataExpiracao = usuarioAplicacao.DataExpiracao,
                NotificacaoEnviada = request.NotificarUsuario,
                ProximaAcaoNecessaria = ObterProximaAcao(request.Decisao, usuarioAplicacao)
            };

            await RegistrarAuditoria("AprovarAcesso", "UsuarioAplicacao", $"{id}-{aplicacaoId}", 
                $"Decisão '{request.Decisao}' para acesso do usuário '{usuario.Email}' à aplicação '{usuarioAplicacao.Aplicacao.Nome}'", 
                null, request);

            // Enviar notificação se solicitado
            if (request.NotificarUsuario && !string.IsNullOrEmpty(usuario.Email))
            {
                await EnviarNotificacaoAprovacao(usuario, usuarioAplicacao.Aplicacao, request);
            }

            _logger.LogInformation("✅ Acesso aprovado - Usuário: {UserId}, Aplicação: {AplicacaoId}, Decisão: {Decisao}, Aprovador: {AprovadorId}", 
                id, aplicacaoId, request.Decisao, usuarioLogadoId);

            return Ok(resposta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao aprovar acesso do usuário {UserId} à aplicação {AplicacaoId}", id, aplicacaoId);
            return StatusCode(500, new RespostaErro 
            { 
                Erro = "ErroInterno", 
                Mensagem = "Erro interno do servidor" 
            });
        }
    }

    /// <summary>
    /// Remove acesso de usuário a uma aplicação
    /// </summary>
    /// <param name="id">ID do usuário</param>
    /// <param name="aplicacaoId">ID da aplicação</param>
    /// <param name="motivo">Motivo da remoção</param>
    /// <returns>Confirmação da remoção</returns>
    [HttpDelete("{id:int}/aplicacoes/{aplicacaoId:int}")]
    [ProducesResponseType(typeof(RespostaSucesso), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoverAcessoAplicacao(int id, int aplicacaoId, [FromQuery] string? motivo)
    {
        try
        {
            if (!TemPermissao("Usuarios.GerenciarAplicacoes"))
            {
                return StatusCode(403, new RespostaErro 
                { 
                    Erro = "AcessoNegado", 
                    Mensagem = "Usuário não tem permissão para remover acesso a aplicações" 
                });
            }

            var usuario = await _userManager.FindByIdAsync(id.ToString());
            if (usuario == null)
            {
                return NotFound(new RespostaErro 
                { 
                    Erro = "UsuarioNaoEncontrado", 
                    Mensagem = "Usuário não encontrado" 
                });
            }

            var usuarioAplicacao = await _context.UsuariosAplicacao
                .Include(ua => ua.Aplicacao)
                .FirstOrDefaultAsync(ua => ua.UsuarioId == id && ua.AplicacaoId == aplicacaoId);

            if (usuarioAplicacao == null)
            {
                return NotFound(new RespostaErro 
                { 
                    Erro = "AcessoNaoEncontrado", 
                    Mensagem = "Usuário não possui acesso a esta aplicação" 
                });
            }

            var nomeAplicacao = usuarioAplicacao.Aplicacao.Nome;

            // Remover acesso
            _context.UsuariosAplicacao.Remove(usuarioAplicacao);
            await _context.SaveChangesAsync();

            await RegistrarAuditoria("RemoverAcesso", "UsuarioAplicacao", $"{id}-{aplicacaoId}", 
                $"Acesso removido do usuário '{usuario.Email}' à aplicação '{nomeAplicacao}'. Motivo: {motivo ?? "Não informado"}", 
                usuarioAplicacao, null);

            _logger.LogInformation("✅ Acesso removido - Usuário: {UserId}, Aplicação: {AplicacaoId}, Admin: {AdminId}", 
                id, aplicacaoId, ObterUsuarioLogadoId());

            return Ok(new RespostaSucesso 
            { 
                Mensagem = $"Acesso à aplicação '{nomeAplicacao}' removido com sucesso" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao remover acesso do usuário {UserId} à aplicação {AplicacaoId}", id, aplicacaoId);
            return StatusCode(500, new RespostaErro 
            { 
                Erro = "ErroInterno", 
                Mensagem = "Erro interno do servidor" 
            });
        }
    }

    /// <summary>
    /// Lista aplicações disponíveis para um usuário solicitar acesso
    /// </summary>
    /// <param name="id">ID do usuário</param>
    /// <param name="filtros">Filtros para aplicações</param>
    /// <returns>Lista de aplicações disponíveis</returns>
    [HttpGet("{id:int}/aplicacoes/disponiveis")]
    [ProducesResponseType(typeof(RespostaPaginada<AplicacaoDisponivelUsuario>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListarAplicacoesDisponiveis(int id, [FromQuery] FiltrosAplicacaoUsuario filtros)
    {
        try
        {
            if (!TemPermissao("Usuarios.Visualizar") && !EhProprioUsuario(id))
            {
                return StatusCode(403, new RespostaErro 
                { 
                    Erro = "AcessoNegado", 
                    Mensagem = "Usuário não tem permissão para visualizar aplicações disponíveis" 
                });
            }

            var usuario = await _userManager.FindByIdAsync(id.ToString());
            if (usuario == null)
            {
                return NotFound(new RespostaErro 
                { 
                    Erro = "UsuarioNaoEncontrado", 
                    Mensagem = "Usuário não encontrado" 
                });
            }

            // Obter aplicações que o usuário NÃO tem acesso
            var aplicacoesComAcesso = await _context.UsuariosAplicacao
                .Where(ua => ua.UsuarioId == id)
                .Select(ua => ua.AplicacaoId)
                .ToListAsync();

            var query = _context.Aplicacoes
                .Include(a => a.TipoAplicacao)
                .Include(a => a.StatusAplicacao)
                .Where(a => a.Ativa && 
                           a.StatusAplicacao.VisivelParaUsuarios &&
                           !aplicacoesComAcesso.Contains(a.Id))
                .AsQueryable();

            // Aplicar filtros básicos
            if (!string.IsNullOrEmpty(filtros.Nome))
            {
                query = query.Where(a => a.Nome.ToLower().Contains(filtros.Nome.ToLower()));
            }

            if (!string.IsNullOrEmpty(filtros.TipoAplicacao))
            {
                query = query.Where(a => a.TipoAplicacao.Codigo == filtros.TipoAplicacao);
            }

            // Paginação
            var totalItens = await query.CountAsync();
            var aplicacoesLista = await query
                .AsNoTracking()
                .Skip((filtros.Pagina - 1) * filtros.ItensPorPagina)
                .Take(filtros.ItensPorPagina)
                .ToListAsync();

            var aplicacoes = aplicacoesLista
                .Select(a => ConstruirAplicacaoDisponivel(a, usuario))
                .ToList();

            var resposta = new RespostaPaginada<AplicacaoDisponivelUsuario>
            {
                Dados = aplicacoes,
                TotalItens = totalItens,
                PaginaAtual = filtros.Pagina,
                ItensPorPagina = filtros.ItensPorPagina,
                TotalPaginas = (int)Math.Ceiling((double)totalItens / filtros.ItensPorPagina)
            };

            return Ok(resposta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao listar aplicações disponíveis para usuário {Id}", id);
            return StatusCode(500, new RespostaErro 
            { 
                Erro = "ErroInterno", 
                Mensagem = "Erro interno do servidor" 
            });
        }
    }

    /// <summary>
    /// Usuário solicita acesso a uma aplicação
    /// </summary>
    /// <param name="id">ID do usuário</param>
    /// <param name="request">Dados da solicitação</param>
    /// <returns>Resultado da solicitação</returns>
    [HttpPost("{id:int}/aplicacoes/solicitar")]
    [ProducesResponseType(typeof(RespostaSolicitacaoAcesso), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SolicitarAcessoAplicacao(int id, [FromBody] SolicitarAcessoAplicacaoRequest request)
    {
        try
        {
            if (!TemPermissao("Usuarios.SolicitarAplicacoes") && !EhProprioUsuario(id))
            {
                return StatusCode(403, new RespostaErro 
                { 
                    Erro = "AcessoNegado", 
                    Mensagem = "Usuário não tem permissão para solicitar acesso a aplicações" 
                });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var usuario = await _userManager.FindByIdAsync(id.ToString());
            if (usuario == null)
            {
                return NotFound(new RespostaErro 
                { 
                    Erro = "UsuarioNaoEncontrado", 
                    Mensagem = "Usuário não encontrado" 
                });
            }

            var aplicacao = await _context.Aplicacoes
                .Include(a => a.TipoAplicacao)
                .Include(a => a.StatusAplicacao)
                .FirstOrDefaultAsync(a => a.Id == request.AplicacaoId);

            if (aplicacao == null)
            {
                return NotFound(new RespostaErro 
                { 
                    Erro = "AplicacaoNaoEncontrada", 
                    Mensagem = "Aplicação não encontrada" 
                });
            }

            // Verificar se já tem acesso
            var jaTemAcesso = await _context.UsuariosAplicacao
                .AnyAsync(ua => ua.UsuarioId == id && ua.AplicacaoId == request.AplicacaoId);

            if (jaTemAcesso)
            {
                return BadRequest(new RespostaErro 
                { 
                    Erro = "AcessoJaExiste", 
                    Mensagem = "Usuário já possui acesso a esta aplicação" 
                });
            }

            var resultadoSolicitacao = await ProcessarSolicitacaoAcesso(usuario, aplicacao, request);

            await RegistrarAuditoria("SolicitarAcesso", "UsuarioAplicacao", $"{id}-{request.AplicacaoId}", 
                $"Usuário '{usuario.Email}' solicitou acesso à aplicação '{aplicacao.Nome}'", 
                null, request);

            _logger.LogInformation("✅ Solicitação de acesso criada - Usuário: {UserId}, Aplicação: {AplicacaoId}", 
                id, request.AplicacaoId);

            return Ok(resultadoSolicitacao);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao processar solicitação de acesso do usuário {UserId} à aplicação {AplicacaoId}", 
                id, request.AplicacaoId);
            return StatusCode(500, new RespostaErro 
            { 
                Erro = "ErroInterno", 
                Mensagem = "Erro interno do servidor" 
            });
        }
    }

    #region Métodos Auxiliares

    /// <summary>
    /// Obtém ID do usuário logado a partir do token
    /// </summary>
    private int ObterUsuarioLogadoId()
    {
        var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(usuarioIdClaim) || !int.TryParse(usuarioIdClaim, out var usuarioId))
        {
            throw new UnauthorizedAccessException("Token inválido ou usuário não identificado");
        }

        return usuarioId;
    }

    /// <summary>
    /// Obtém dados completos de um usuário com relacionamentos
    /// </summary>
    private async Task<UsuarioCompleto> ObterUsuarioCompletoAsync(int usuarioId)
    {
        var usuario = await _context.Users
            .Include(u => u.UsuarioPapeis.Where(up => up.Ativo))
                .ThenInclude(up => up.Papel)
            .Include(u => u.UsuarioGrupos.Where(ug => ug.Ativo))
                .ThenInclude(ug => ug.Grupo)
            .AsSplitQuery()
            .FirstOrDefaultAsync(u => u.Id == usuarioId);

        if (usuario == null)
        {
            throw new InvalidOperationException($"Usuário com ID {usuarioId} não encontrado");
        }

        // ✅ OBTER PERMISSÕES DO USUÁRIO
        var permissoes = await ObterPermissoesUsuarioAsync(usuarioId);

        return new UsuarioCompleto
        {
            Id = usuario.Id,
            Email = usuario.Email!,
            Nome = usuario.Nome,
            Sobrenome = usuario.Sobrenome,
            NomeCompleto = usuario.NomeCompleto ?? $"{usuario.Nome} {usuario.Sobrenome}",
            Telefone = usuario.PhoneNumber,
            EmailConfirmado = usuario.EmailConfirmed,
            TelefoneConfirmado = usuario.PhoneNumberConfirmed,
            Ativo = usuario.Ativo,
            Observacoes = usuario.Observacoes,
            DataCriacao = usuario.DataCriacao,
            DataAtualizacao = usuario.DataAtualizacao,
            UltimoLogin = usuario.UltimoLogin,
            
            // ✅ MAPEAR PAPÉIS
            Papeis = usuario.UsuarioPapeis
                .Where(up => up.Ativo && up.Papel.Ativo)
                .Select(up => new PapelUsuario
                {
                    Id = up.Papel.Id,
                    Nome = up.Papel.Name!,
                    Descricao = up.Papel.Descricao,
                    Categoria = up.Papel.Categoria,
                    DataAtribuicao = up.DataAtribuicao,
                    DataExpiracao = up.DataExpiracao
                })
                .OrderBy(p => p.Nome)
                .ToList(),

            // ✅ MAPEAR PERMISSÕES
            Permissoes = permissoes
                .Select(p => new PermissaoUsuario
                {
                    Id = p.Id,
                    Nome = p.Nome,
                    Descricao = p.Descricao,
                    Recurso = p.Recurso,
                    Acao = p.Acao,
                    Categoria = p.Categoria ?? "Sistema",
                    OrigemPapel = p.OrigemPapel ?? "Direto"
                })
                .OrderBy(p => p.Recurso)
                .ThenBy(p => p.Acao)
                .ToList(),

            // ✅ MAPEAR GRUPOS
            Grupos = usuario.UsuarioGrupos
                .Where(ug => ug.Ativo && ug.Grupo.Ativo)
                .Select(ug => new GrupoUsuario
                {
                    Id = ug.Grupo.Id,
                    Nome = ug.Grupo.Nome,
                    Descricao = ug.Grupo.Descricao,
                    Tipo = ug.Grupo.Tipo,
                    DataAdesao = ug.DataAdesao
                })
                .OrderBy(g => g.Nome)
                .ToList(),

            // ✅ ESTATÍSTICAS DO USUÁRIO
            Estatisticas = new EstatisticasUsuario
            {
                TotalPapeis = usuario.UsuarioPapeis.Count(up => up.Ativo),
                TotalPermissoes = permissoes.Count,
                TotalGrupos = usuario.UsuarioGrupos.Count(ug => ug.Ativo),
                ContadorLogins = 0, // TODO: Implementar contador de logins se necessário
                UltimoPapelAtribuido = usuario.UsuarioPapeis
                    .Where(up => up.Ativo)
                    .OrderByDescending(up => up.DataAtribuicao)
                    .FirstOrDefault()?.DataAtribuicao
            }
        };
    }

    /// <summary>
    /// Obtém todas as permissões de um usuário através dos papéis
    /// </summary>
    private async Task<List<PermissaoCompleta>> ObterPermissoesUsuarioAsync(int usuarioId)
    {
        var permissoes = await _context.Users
            .Where(u => u.Id == usuarioId)
            .SelectMany(u => u.UsuarioPapeis)
            .Where(up => up.Ativo && up.Papel.Ativo)
            .SelectMany(up => up.Papel.PapelPermissoes)
            .Where(pp => pp.Ativo && pp.Permissao.Ativo)
            .Select(pp => new PermissaoCompleta
            {
                Id = pp.Permissao.Id,
                Nome = pp.Permissao.Nome,
                Descricao = pp.Permissao.Descricao,
                Recurso = pp.Permissao.Recurso,
                Acao = pp.Permissao.Acao,
                Categoria = pp.Permissao.Categoria,
                OrigemPapel = pp.Papel.Name
            })
            .Distinct()
            .ToListAsync();

        return permissoes;
    }

    /// <summary>
    /// Verifica se o usuário logado tem uma permissão específica
    /// </summary>
    private bool TemPermissao(string permissao)
    {
        // ✅ VERIFICAR CLAIMS DE PERMISSÃO NO TOKEN
        var permissoesClaims = User.FindAll("permissao").Select(c => c.Value);
        
        // ✅ VERIFICAR SE TEM A PERMISSÃO ESPECÍFICA OU É SUPERADMIN
        return permissoesClaims.Contains(permissao) || 
               User.IsInRole("SuperAdmin") || 
               permissoesClaims.Contains("*"); // Permissão universal
    }

    /// <summary>
    /// Aplica filtros dinâmicos à query
    /// </summary>
    private IQueryable<Usuario> AplicarFiltros(IQueryable<Usuario> query, FiltrosUsuario filtros)
    {
        if (!string.IsNullOrEmpty(filtros.Email))
        {
            query = query.Where(u => u.Email!.Contains(filtros.Email));
        }

        if (!string.IsNullOrEmpty(filtros.Nome))
        {
            query = query.Where(u => u.Nome.Contains(filtros.Nome) ||
                                   u.Sobrenome.Contains(filtros.Nome) ||
                                   (u.NomeCompleto != null && u.NomeCompleto.Contains(filtros.Nome)));
        }

        if (filtros.Ativo.HasValue)
        {
            query = query.Where(u => u.Ativo == filtros.Ativo.Value);
        }

        if (filtros.DataCriacaoInicio.HasValue)
        {
            query = query.Where(u => u.DataCriacao >= filtros.DataCriacaoInicio.Value);
        }

        if (filtros.DataCriacaoFim.HasValue)
        {
            query = query.Where(u => u.DataCriacao <= filtros.DataCriacaoFim.Value);
        }

        if (filtros.Papeis?.Any() == true)
        {
            query = query.Where(u => u.UsuarioPapeis
                .Any(up => up.Ativo && filtros.Papeis.Contains(up.Papel.Name!)));
        }

        return query;
    }

    /// <summary>
    /// Aplica ordenação dinâmica
    /// </summary>
    private IQueryable<Usuario> AplicarOrdenacao(IQueryable<Usuario> query, string? ordenarPor, string? direcao)
    {
        if (string.IsNullOrEmpty(ordenarPor))
            ordenarPor = "DataCriacao";

        var ascendente = string.IsNullOrEmpty(direcao) || direcao.ToLower() == "asc";

        return ordenarPor.ToLower() switch
        {
            "email" => ascendente ? query.OrderBy(u => u.Email) : query.OrderByDescending(u => u.Email),
            "nome" => ascendente ? query.OrderBy(u => u.Nome).ThenBy(u => u.Sobrenome) : query.OrderByDescending(u => u.Nome).ThenByDescending(u => u.Sobrenome),
            "datacriacao" => ascendente ? query.OrderBy(u => u.DataCriacao) : query.OrderByDescending(u => u.DataCriacao),
            "ultimologin" => ascendente ? query.OrderBy(u => u.UltimoLogin) : query.OrderByDescending(u => u.UltimoLogin),
            "ativo" => ascendente ? query.OrderBy(u => u.Ativo) : query.OrderByDescending(u => u.Ativo),
            _ => query.OrderByDescending(u => u.DataCriacao)
        };
    }

    /// <summary>
    /// Registra uma ação na auditoria
    /// </summary>
    private async Task RegistrarAuditoria(string acao, string recurso, string? recursoId, 
        string observacoes, object? dadosAntes = null, object? dadosDepois = null)
    {
        try
        {
            var usuarioId = ObterUsuarioLogadoId();
            var enderecoIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            var registro = new RegistroAuditoria
            {
                UsuarioId = usuarioId,
                Acao = acao,
                Recurso = recurso,
                RecursoId = recursoId,
                EnderecoIp = enderecoIp,
                UserAgent = userAgent?.Substring(0, Math.Min(userAgent.Length, 500)),
                Observacoes = observacoes?.Substring(0, Math.Min(observacoes.Length, 500)),
                DadosAntes = dadosAntes != null ? JsonSerializer.Serialize(dadosAntes) : null,
                DadosDepois = dadosDepois != null ? JsonSerializer.Serialize(dadosDepois) : null,
                DataHora = DateTime.UtcNow
            };

            _context.RegistrosAuditoria.Add(registro);
            await _context.SaveChangesAsync();

            _logger.LogInformation("📝 Auditoria registrada - {Acao} em {Recurso}:{RecursoId} por usuário {UsuarioId}", 
                acao, recurso, recursoId ?? "N/A", usuarioId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao registrar auditoria");
            // Não fazer throw para não afetar a operação principal
        }
    }

    /// <summary>
    /// Conta logins do usuário (para estatísticas)
    /// </summary>
    private async Task<int> ContarLoginsUsuario(int usuarioId)
    {
        return await _context.RegistrosAuditoria
            .Where(ra => ra.UsuarioId == usuarioId &&
                        ra.Acao == "Login" &&
                        ra.DataHora >= DateTime.UtcNow.AddDays(-30))
            .CountAsync();
    }

    /// <summary>
    /// Obtém dados completos do usuário (para responses)
    /// </summary>
    private async Task<UsuarioCompleto> ObterDadosCompletos(int usuarioId)
    {
        // Implementação similar ao método ObterUsuario, mas retornando apenas os dados
        // Reutilizar lógica do método GET
        var actionResult = await ObterUsuario(usuarioId);
        return (actionResult as OkObjectResult)?.Value as UsuarioCompleto ?? new UsuarioCompleto();
    }

    /// <summary>
    /// Gerencia papéis de um usuário (adiciona/remove)
    /// </summary>
    private async Task GerenciarPapeisUsuario(Usuario usuario, List<string> novoPapeis, int usuarioLogadoId)
    {
        // Obter papéis atuais
        var papeisAtuais = await _userManager.GetRolesAsync(usuario);

        // Papéis para adicionar
        var papeisParaAdicionar = novoPapeis.Except(papeisAtuais).ToList();

        // Papéis para remover
        var papeisParaRemover = papeisAtuais.Except(novoPapeis).ToList();

        // ✅ VERIFICAR SE OS PAPÉIS EXISTEM E ESTÃO ATIVOS
        if (papeisParaAdicionar.Any())
        {
            var papeisValidos = await _roleManager.Roles
                .Where(r => papeisParaAdicionar.Contains(r.Name!) && r.Ativo)
                .Select(r => r.Name!)
                .ToListAsync();

            if (papeisValidos.Any())
            {
                var resultado = await _userManager.AddToRolesAsync(usuario, papeisValidos);
                if (!resultado.Succeeded)
                {
                    throw new InvalidOperationException($"Erro ao adicionar papéis: {string.Join(", ", resultado.Errors.Select(e => e.Description))}");
                }

                // ✅ ATUALIZAR REGISTROS DE USUARIOPAPEL COM METADADOS
                foreach (var papel in papeisValidos)
                {
                    var papelEntity = await _roleManager.FindByNameAsync(papel);
                    var usuarioPapel = await _context.Set<UsuarioPapel>()
                        .FirstOrDefaultAsync(up => up.UserId == usuario.Id && up.PapelId == papelEntity!.Id);

                    if (usuarioPapel != null)
                    {
                        usuarioPapel.DataAtribuicao = DateTime.UtcNow;
                        usuarioPapel.AtribuidoPorId = usuarioLogadoId;
                        usuarioPapel.Ativo = true;
                    }
                }
            }
        }

        // ✅ REMOVER PAPÉIS (soft delete)
        if (papeisParaRemover.Any())
        {
            // ✅ NÃO PERMITIR REMOÇÃO DO PRÓPRIO SUPER ADMIN
            if (usuario.Id == usuarioLogadoId && papeisParaRemover.Contains("SuperAdmin"))
            {
                throw new InvalidOperationException("Não é possível remover o próprio papel de Super Admin");
            }

            var resultado = await _userManager.RemoveFromRolesAsync(usuario, papeisParaRemover);
            if (!resultado.Succeeded)
            {
                throw new InvalidOperationException($"Erro ao remover papéis: {string.Join(", ", resultado.Errors.Select(e => e.Description))}");
            }
        }
    }

    /// <summary>
    /// Constrói objeto UsuarioCompleto a partir de uma entidade Usuario
    /// </summary>
    private async Task<UsuarioCompleto> ConstruirUsuarioCompleto(Usuario usuario)
    {
        var papeis = usuario.UsuarioPapeis
            .Where(up => up.Ativo && up.Papel.Ativo)
            .Select(up => new PapelUsuario
            {
                Id = up.Papel.Id,
                Nome = up.Papel.Name!,
                Descricao = up.Papel.Descricao,
                Categoria = up.Papel.Categoria,
                DataAtribuicao = up.DataAtribuicao,
                DataExpiracao = up.DataExpiracao
            })
            .ToList();

        var permissoes = usuario.UsuarioPapeis
            .Where(up => up.Ativo && up.Papel.Ativo)
            .SelectMany(up => up.Papel.PapelPermissoes)
            .Where(pp => pp.Ativo && pp.Permissao.Ativo)
            .Select(pp => new PermissaoUsuario
            {
                Id = pp.Permissao.Id,
                Nome = pp.Permissao.Nome,
                Descricao = pp.Permissao.Descricao,
                Recurso = pp.Permissao.Recurso,
                Acao = pp.Permissao.Acao,
                Categoria = pp.Permissao.Categoria,
                OrigemPapel = pp.Papel.Name!
            })
            .DistinctBy(p => p.Nome)
            .ToList();

        var grupos = usuario.UsuarioGrupos
            .Where(ug => ug.Ativo && ug.Grupo.Ativo)
            .Select(ug => new GrupoUsuario
            {
                Id = ug.Grupo.Id,
                Nome = ug.Grupo.Nome,
                Descricao = ug.Grupo.Descricao,
                Tipo = ug.Grupo.Tipo,
                DataAdesao = ug.DataAdesao
            })
            .ToList();

        return new UsuarioCompleto
        {
            Id = usuario.Id,
            Email = usuario.Email!,
            Nome = usuario.Nome,
            Sobrenome = usuario.Sobrenome,
            NomeCompleto = usuario.NomeCompleto ?? $"{usuario.Nome} {usuario.Sobrenome}",
            Telefone = usuario.PhoneNumber,
            EmailConfirmado = usuario.EmailConfirmed,
            TelefoneConfirmado = usuario.PhoneNumberConfirmed,
            Ativo = usuario.Ativo,
            Observacoes = usuario.Observacoes,
            DataCriacao = usuario.DataCriacao,
            DataAtualizacao = usuario.DataAtualizacao,
            UltimoLogin = usuario.UltimoLogin,
            Papeis = papeis,
            Permissoes = permissoes,
            Grupos = grupos,
            Estatisticas = new EstatisticasUsuario
            {
                TotalPapeis = papeis.Count,
                TotalPermissoes = permissoes.Count,
                TotalGrupos = grupos.Count,
                ContadorLogins = await ContarLoginsUsuario(usuario.Id),
                UltimoPapelAtribuido = papeis.OrderByDescending(p => p.DataAtribuicao).FirstOrDefault()?.DataAtribuicao
            }
        };
    }

    #endregion

    #region Métodos Auxiliares de Remoção

    /// <summary>
    /// Valida se o usuário pode ser removido
    /// </summary>
    private async Task<IActionResult?> ValidarRemocaoUsuario(Usuario usuario, int usuarioLogadoId, bool exclusaoPermanente)
    {
        // ✅ NÃO PERMITIR AUTO-REMOÇÃO
        if (usuario.Id == usuarioLogadoId)
        {
            return BadRequest(new RespostaErro
            {
                Erro = "OperacaoInvalida",
                Mensagem = "Não é possível remover seu próprio usuário"
            });
        }

        // ✅ VERIFICAR SE É SUPER ADMIN
        var papeis = await _userManager.GetRolesAsync(usuario);
        if (papeis.Contains("SuperAdmin"))
        {
            // Contar quantos Super Admins existem
            var totalSuperAdmins = await _context.Users
                .Where(u => u.UsuarioPapeis.Any(up => up.Papel.Name == "SuperAdmin" && up.Ativo) && u.Ativo)
                .CountAsync();

            if (totalSuperAdmins <= 1)
            {
                return BadRequest(new RespostaErro
                {
                    Erro = "UltimoSuperAdmin",
                    Mensagem = "Não é possível remover o último Super Administrador do sistema"
                });
            }

            // ✅ SUPER ADMIN só pode ser removido por outro SUPER ADMIN
            var usuarioLogado = await _context.Users
                .Include(u => u.UsuarioPapeis)
                    .ThenInclude(up => up.Papel)
                .FirstOrDefaultAsync(u => u.Id == usuarioLogadoId);

            var usuarioLogadoEhSuperAdmin = usuarioLogado?.UsuarioPapeis
                .Any(up => up.Papel.Name == "SuperAdmin" && up.Ativo) ?? false;

            if (!usuarioLogadoEhSuperAdmin)
            {
                return Forbid("Apenas Super Administradores podem remover outros Super Administradores");
            }
        }

        // ✅ VERIFICAR DEPENDÊNCIAS PARA EXCLUSÃO PERMANENTE
        if (exclusaoPermanente)
        {
            var temRegistrosAuditoria = await _context.RegistrosAuditoria
                .AnyAsync(ra => ra.UsuarioId == usuario.Id);

            var temUsuariosAtribuidos = await _context.Set<UsuarioPapel>()
                .AnyAsync(up => up.AtribuidoPorId == usuario.Id);

            if (temRegistrosAuditoria || temUsuariosAtribuidos)
            {
                var detalhes = new List<string>();
                if (temRegistrosAuditoria)
                    detalhes.Add("Possui registros de auditoria");
                if (temUsuariosAtribuidos)
                    detalhes.Add("Possui usuários atribuídos por ele");

                return BadRequest(new RespostaErro
                {
                    Erro = "UsuarioComDependencias",
                    Mensagem = "Usuário não pode ser excluído permanentemente pois possui registros de auditoria ou usuários atribuídos. Use desativação em vez de exclusão.",
                    Detalhes = detalhes
                });
            }
        }

        return null; // Validação passou
    }

    /// <summary>
    /// Realiza desativação do usuário (soft delete)
    /// </summary>
    private async Task RealizarDesativacao(Usuario usuario)
    {
        // ✅ DESATIVAR USUÁRIO
        usuario.Ativo = false;
        usuario.DataAtualizacao = DateTime.UtcNow;

        // ✅ DESATIVAR RELACIONAMENTOS
        foreach (var usuarioPapel in usuario.UsuarioPapeis.Where(up => up.Ativo))
        {
            usuarioPapel.Ativo = false;
        }

        foreach (var usuarioGrupo in usuario.UsuarioGrupos.Where(ug => ug.Ativo))
        {
            usuarioGrupo.Ativo = false;
        }

        // ✅ CORRIGIDO: NÃO usar UserManager dentro de transação - salvar direto pelo contexto
        _context.Users.Update(usuario);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Realiza exclusão permanente do usuário (hard delete)
    /// </summary>
    private async Task RealizarExclusaoPermanente(Usuario usuario)
    {
        // ✅ REMOVER RELACIONAMENTOS PRIMEIRO (ordem importante)

        // 1. Remover relacionamentos de grupos
        var usuarioGrupos = await _context.Set<UsuarioGrupo>()
            .Where(ug => ug.UsuarioId == usuario.Id)
            .ToListAsync();

        if (usuarioGrupos.Any())
        {
            _context.Set<UsuarioGrupo>().RemoveRange(usuarioGrupos);
        }

        // 2. Remover relacionamentos de papéis
        var usuarioPapeis = await _context.Set<UsuarioPapel>()
            .Where(up => up.UserId == usuario.Id)
            .ToListAsync();

        if (usuarioPapeis.Any())
        {
            _context.Set<UsuarioPapel>().RemoveRange(usuarioPapeis);
        }

        // 3. ⚠️ IMPORTANTE: Atualizar registros de auditoria (não remover, apenas desassociar)
        var registrosAuditoria = await _context.RegistrosAuditoria
            .Where(ra => ra.UsuarioId == usuario.Id)
            .ToListAsync();

        foreach (var registro in registrosAuditoria)
        {
            registro.UsuarioId = null; // Desassociar mas manter histórico
            if (string.IsNullOrEmpty(registro.Observacoes))
            {
                registro.Observacoes = $"Usuário {usuario.Email} foi excluído permanentemente";
            }
            else
            {
                registro.Observacoes += $" | Usuário {usuario.Email} foi excluído permanentemente";
            }
        }

        // 4. Atualizar registros onde este usuário atribuiu papéis
        var atribuicoesFeitas = await _context.Set<UsuarioPapel>()
            .Where(up => up.AtribuidoPorId == usuario.Id)
            .ToListAsync();

        foreach (var atribuicao in atribuicoesFeitas)
        {
            atribuicao.AtribuidoPorId = null; // Desassociar mas manter histórico
        }

        // 5. Remover claims, logins e tokens do Identity
        var userClaims = await _context.UserClaims.Where(uc => uc.UserId == usuario.Id).ToListAsync();
        if (userClaims.Any())
        {
            _context.UserClaims.RemoveRange(userClaims);
        }

        var userLogins = await _context.UserLogins.Where(ul => ul.UserId == usuario.Id).ToListAsync();
        if (userLogins.Any())
        {
            _context.UserLogins.RemoveRange(userLogins);
        }

        var userTokens = await _context.UserTokens.Where(ut => ut.UserId == usuario.Id).ToListAsync();
        if (userTokens.Any())
        {
            _context.UserTokens.RemoveRange(userTokens);
        }

        // 6. Finalmente, excluir o usuário
        _context.Users.Remove(usuario);
        
        // 7. Salvar todas as alterações
        await _context.SaveChangesAsync();
    }

    #endregion

    #region Métodos Auxiliares de Gerenciamento de Papéis

    /// <summary>
    /// Valida operações de papéis antes da execução
    /// </summary>
    private async Task<IActionResult?> ValidarOperacaoPapeis(Usuario usuario, GerenciarPapeisRequest request, int usuarioLogadoId)
    {
        // ✅ VALIDAR SE USUÁRIO ESTÁ ATIVO
        if (!usuario.Ativo)
        {
            return BadRequest(new RespostaErro
            {
                Erro = "UsuarioInativo",
                Mensagem = "Não é possível gerenciar papéis de usuário inativo"
            });
        }

        // ✅ VALIDAR AUTO-REMOÇÃO DE SUPER ADMIN
        if (usuario.Id == usuarioLogadoId &&
            (request.Operacao.ToLower() == "remover" || request.Operacao.ToLower() == "limpar" || request.Operacao.ToLower() == "substituir"))
        {
            var usuarioLogadoPapeis = await _userManager.GetRolesAsync(usuario);
            var temSuperAdmin = usuarioLogadoPapeis.Contains("SuperAdmin");

            if (temSuperAdmin)
            {
                // Verificar se está tentando remover SuperAdmin
                bool tentandoRemoverSuperAdmin = false;

                if (request.Operacao.ToLower() == "remover" && request.Papeis.Contains("SuperAdmin"))
                {
                    tentandoRemoverSuperAdmin = true;
                }
                else if (request.Operacao.ToLower() == "substituir" && !request.Papeis.Contains("SuperAdmin"))
                {
                    tentandoRemoverSuperAdmin = true;
                }
                else if (request.Operacao.ToLower() == "limpar")
                {
                    tentandoRemoverSuperAdmin = true;
                }

                if (tentandoRemoverSuperAdmin)
                {
                    // Verificar se é o último Super Admin
                    var totalSuperAdmins = await _context.Users
                        .Where(u => u.UsuarioPapeis.Any(up => up.Papel.Name == "SuperAdmin" && up.Ativo) && u.Ativo)
                        .CountAsync();

                    if (totalSuperAdmins <= 1)
                    {
                        return BadRequest(new RespostaErro
                        {
                            Erro = "UltimoSuperAdmin",
                            Mensagem = "Não é possível remover o papel SuperAdmin do último Super Administrador"
                        });
                    }
                }
            }
        }

        // ✅ VALIDAR SE PAPÉIS EXISTEM E ESTÃO ATIVOS
        if (request.Papeis?.Any() == true)
        {
            var papeisInvalidos = new List<string>();

            foreach (var nomePapel in request.Papeis)
            {
                var papel = await _roleManager.FindByNameAsync(nomePapel);
                if (papel == null || !papel.Ativo)
                {
                    papeisInvalidos.Add(nomePapel);
                }
            }

            if (papeisInvalidos.Any())
            {
                return BadRequest(new RespostaErro
                {
                    Erro = "PapeisInvalidos",
                    Mensagem = "Alguns papéis são inválidos ou estão inativos",
                    Detalhes = papeisInvalidos.Select(p => $"Papel inválido: {p}").ToList()
                });
            }
        }

        return null; // Validação passou
    }

    /// <summary>
    /// Substitui todos os papéis do usuário pelos especificados
    /// </summary>
    private async Task<ResultadoOperacao> SubstituirPapeis(Usuario usuario, List<string> novosPapeis, int usuarioLogadoId)
    {
        try
        {
            // Remover todos os papéis atuais
            var papeisAtuais = await _userManager.GetRolesAsync(usuario);
            if (papeisAtuais.Any())
            {
                var resultadoRemocao = await _userManager.RemoveFromRolesAsync(usuario, papeisAtuais);
                if (!resultadoRemocao.Succeeded)
                {
                    return new ResultadoOperacao
                    {
                        Sucesso = false,
                        Mensagem = "Erro ao remover papéis existentes",
                        Detalhes = resultadoRemocao.Errors.Select(e => e.Description).ToList()
                    };
                }
            }

            // Adicionar novos papéis
            if (novosPapeis.Any())
            {
                var resultadoAdicao = await _userManager.AddToRolesAsync(usuario, novosPapeis);
                if (!resultadoAdicao.Succeeded)
                {
                    return new ResultadoOperacao
                    {
                        Sucesso = false,
                        Mensagem = "Erro ao adicionar novos papéis",
                        Detalhes = resultadoAdicao.Errors.Select(e => e.Description).ToList()
                    };
                }

                // Atualizar metadados dos relacionamentos
                await AtualizarMetadadosPapeis(usuario.Id, novosPapeis, usuarioLogadoId);
            }

            return new ResultadoOperacao { Sucesso = true, Mensagem = "Papéis substituídos com sucesso" };
        }
        catch (Exception ex)
        {
            return new ResultadoOperacao
            {
                Sucesso = false,
                Mensagem = "Erro interno ao substituir papéis",
                Detalhes = new List<string> { ex.Message }
            };
        }
    }

    /// <summary>
    /// Adiciona papéis específicos ao usuário
    /// </summary>
    private async Task<ResultadoOperacao> AdicionarPapeis(Usuario usuario, List<string> papeisParaAdicionar, int usuarioLogadoId)
    {
        try
        {
            var papeisAtuais = await _userManager.GetRolesAsync(usuario);
            var papeisNovos = papeisParaAdicionar.Except(papeisAtuais).ToList();

            if (!papeisNovos.Any())
            {
                return new ResultadoOperacao
                {
                    Sucesso = true,
                    Mensagem = "Usuário já possui todos os papéis especificados"
                };
            }

            var resultado = await _userManager.AddToRolesAsync(usuario, papeisNovos);
            if (!resultado.Succeeded)
            {
                return new ResultadoOperacao
                {
                    Sucesso = false,
                    Mensagem = "Erro ao adicionar papéis",
                    Detalhes = resultado.Errors.Select(e => e.Description).ToList()
                };
            }

            // Atualizar metadados
            await AtualizarMetadadosPapeis(usuario.Id, papeisNovos, usuarioLogadoId);

            return new ResultadoOperacao
            {
                Sucesso = true,
                Mensagem = $"{papeisNovos.Count} papéis adicionados com sucesso"
            };
        }
        catch (Exception ex)
        {
            return new ResultadoOperacao
            {
                Sucesso = false,
                Mensagem = "Erro interno ao adicionar papéis",
                Detalhes = new List<string> { ex.Message }
            };
        }
    }

    /// <summary>
    /// Remove papéis específicos do usuário
    /// </summary>
    private async Task<ResultadoOperacao> RemoverPapeis(Usuario usuario, List<string> papeisParaRemover, int usuarioLogadoId)
    {
        try
        {
            var papeisAtuais = await _userManager.GetRolesAsync(usuario);
            var papeisExistentes = papeisParaRemover.Intersect(papeisAtuais).ToList();

            if (!papeisExistentes.Any())
            {
                return new ResultadoOperacao
                {
                    Sucesso = true,
                    Mensagem = "Usuário não possui os papéis especificados para remoção"
                };
            }

            var resultado = await _userManager.RemoveFromRolesAsync(usuario, papeisExistentes);
            if (!resultado.Succeeded)
            {
                return new ResultadoOperacao
                {
                    Sucesso = false,
                    Mensagem = "Erro ao remover papéis",
                    Detalhes = resultado.Errors.Select(e => e.Description).ToList()
                };
            }

            return new ResultadoOperacao
            {
                Sucesso = true,
                Mensagem = $"{papeisExistentes.Count} papéis removidos com sucesso"
            };
        }
        catch (Exception ex)
        {
            return new ResultadoOperacao
            {
                Sucesso = false,
                Mensagem = "Erro interno ao remover papéis",
                Detalhes = new List<string> { ex.Message }
            };
        }
    }

    /// <summary>
    /// Remove todos os papéis do usuário
    /// </summary>
    private async Task<ResultadoOperacao> LimparTodosPapeis(Usuario usuario, int usuarioLogadoId)
    {
        try
        {
            var papeisAtuais = await _userManager.GetRolesAsync(usuario);

            if (!papeisAtuais.Any())
            {
                return new ResultadoOperacao
                {
                    Sucesso = true,
                    Mensagem = "Usuário não possui papéis para remover"
                };
            }

            var resultado = await _userManager.RemoveFromRolesAsync(usuario, papeisAtuais);
            if (!resultado.Succeeded)
            {
                return new ResultadoOperacao
                {
                    Sucesso = false,
                    Mensagem = "Erro ao remover todos os papéis",
                    Detalhes = resultado.Errors.Select(e => e.Description).ToList()
                };
            }

            return new ResultadoOperacao
            {
                Sucesso = true,
                Mensagem = $"Todos os {papeisAtuais.Count} papéis foram removidos"
            };
        }
        catch (Exception ex)
        {
            return new ResultadoOperacao
            {
                Sucesso = false,
                Mensagem = "Erro interno ao limpar papéis",
                Detalhes = new List<string> { ex.Message }
            };
        }
    }

    /// <summary>
    /// Atualiza metadados dos relacionamentos de papéis
    /// </summary>
    private async Task AtualizarMetadadosPapeis(int usuarioId, List<string> papeis, int atribuidoPorId)
    {
        try
        {
            var relacionamentos = await _context.Set<UsuarioPapel>()
                .Where(up => up.UserId == usuarioId && papeis.Contains(up.Papel.Name!))
                .Include(up => up.Papel)
                .ToListAsync();

            foreach (var relacionamento in relacionamentos)
            {
                relacionamento.AtribuidoPorId = atribuidoPorId;
                relacionamento.DataAtribuicao = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao atualizar metadados de papéis para usuário {UsuarioId}", usuarioId);
        }
    }

    #endregion

    #region Métodos Auxiliares de Busca Avançada

    /// <summary>
    /// Aplica filtros avançados à query
    /// </summary>
    private async Task<IQueryable<Usuario>> AplicarFiltrosAvancados(IQueryable<Usuario> query, SolicitacaoBuscaAvancada request)
    {
        // ✅ FILTRO POR TEXTO GERAL (busca em múltiplos campos)
        if (!string.IsNullOrEmpty(request.TextoGeral))
        {
            var texto = request.TextoGeral.ToLower();
            query = query.Where(u =>
                u.Email!.ToLower().Contains(texto) ||
                u.Nome.ToLower().Contains(texto) ||
                u.Sobrenome.ToLower().Contains(texto) ||
                (u.NomeCompleto != null && u.NomeCompleto.ToLower().Contains(texto)) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(texto)));
        }

        // ✅ FILTROS ESPECÍFICOS
        if (!string.IsNullOrEmpty(request.Email))
        {
            query = query.Where(u => u.Email!.ToLower().Contains(request.Email.ToLower()));
        }

        if (!string.IsNullOrEmpty(request.Nome))
        {
            var nome = request.Nome.ToLower();
            query = query.Where(u => u.Nome.ToLower().Contains(nome) || u.Sobrenome.ToLower().Contains(nome));
        }

        if (!string.IsNullOrEmpty(request.Telefone))
        {
            query = query.Where(u => u.PhoneNumber != null && u.PhoneNumber.Contains(request.Telefone));
        }

        // ✅ FILTROS DE STATUS
        if (request.Ativo.HasValue)
        {
            query = query.Where(u => u.Ativo == request.Ativo.Value);
        }

        if (request.EmailConfirmado.HasValue)
        {
            query = query.Where(u => u.EmailConfirmed == request.EmailConfirmado.Value);
        }

        if (request.TelefoneConfirmado.HasValue)
        {
            query = query.Where(u => u.PhoneNumberConfirmed == request.TelefoneConfirmado.Value);
        }

        // ✅ FILTROS DE DATA
        if (request.DataCriacaoInicio.HasValue)
        {
            query = query.Where(u => u.DataCriacao >= request.DataCriacaoInicio.Value);
        }

        if (request.DataCriacaoFim.HasValue)
        {
            query = query.Where(u => u.DataCriacao <= request.DataCriacaoFim.Value);
        }

        if (request.UltimoLoginInicio.HasValue)
        {
            query = query.Where(u => u.UltimoLogin >= request.UltimoLoginInicio.Value);
        }

        if (request.UltimoLoginFim.HasValue)
        {
            query = query.Where(u => u.UltimoLogin <= request.UltimoLoginFim.Value);
        }

        // ✅ FILTRAR POR PAPÉIS
        if (request.Papeis?.Any() == true)
        {
            if (request.OperadorPapeis == "OU")
            {
                // Usuário deve ter QUALQUER um dos papéis
                query = query.Where(u => u.UsuarioPapeis
                    .Any(up => up.Ativo && request.Papeis.Contains(up.Papel.Name!)));
            }
            else // "E" - padrão
            {
                // Usuário deve ter TODOS os papéis
                foreach (var papel in request.Papeis)
                {
                    query = query.Where(u => u.UsuarioPapeis
                        .Any(up => up.Ativo && up.Papel.Name == papel));
                }
            }
        }

        // ✅ FILTRAR POR GRUPOS
        if (request.Grupos?.Any() == true)
        {
            if (request.OperadorGrupos == "OU")
            {
                query = query.Where(u => u.UsuarioGrupos
                    .Any(ug => ug.Ativo && request.Grupos.Contains(ug.Grupo.Nome)));
            }
            else // "E" - padrão
            {
                foreach (var grupo in request.Grupos)
                {
                    query = query.Where(u => u.UsuarioGrupos
                        .Any(ug => ug.Ativo && ug.Grupo.Nome == grupo));
                }
            }
        }

        // ✅ FILTRAR POR PERMISSÕES (através dos papéis)
        if (request.Permissoes?.Any() == true)
        {
            var permissoesIds = await _context.Permissoes
                .Where(p => request.Permissoes.Contains(p.Nome) && p.Ativo)
                .Select(p => p.Id)
                .ToListAsync();

            if (permissoesIds.Any())
            {
                if (request.OperadorPermissoes == "OU")
                {
                    query = query.Where(u => u.UsuarioPapeis
                        .Any(up => up.Ativo && up.Papel.PapelPermissoes
                            .Any(pp => pp.Ativo && permissoesIds.Contains(pp.PermissaoId))));
                }
                else // "E" - padrão
                {
                    foreach (var permissaoId in permissoesIds)
                    {
                        query = query.Where(u => u.UsuarioPapeis
                            .Any(up => up.Ativo && up.Papel.PapelPermissoes
                                .Any(pp => pp.Ativo && pp.PermissaoId == permissaoId)));
                    }
                }
            }
        }

        // ✅ FILTROS AVANÇADOS
        if (request.SemPapeis == true)
        {
            query = query.Where(u => !u.UsuarioPapeis.Any(up => up.Ativo));
        }

        if (request.SemGrupos == true)
        {
            query = query.Where(u => !u.UsuarioGrupos.Any(ug => ug.Ativo));
        }

        if (request.SemUltimoLogin == true)
        {
            query = query.Where(u => u.UltimoLogin == null);
        }

        return query;
    }

    /// <summary>
    /// Aplica ordenação avançada à query
    /// </summary>
    private IQueryable<Usuario> AplicarOrdenacaoAvancada(IQueryable<Usuario> query, List<CriterioOrdenacao>? criterios)
    {
        if (criterios?.Any() != true)
        {
            // Ordenação padrão
            return query.OrderByDescending(u => u.DataCriacao);
        }

        IOrderedQueryable<Usuario>? queryOrdenada = null;

        for (int i = 0; i < criterios.Count; i++)
        {
            var criterio = criterios[i];
            var ascendente = criterio.Direcao?.ToLower() != "desc";

            if (i == 0)
            {
                // Primeira ordenação
                queryOrdenada = criterio.Campo.ToLower() switch
                {
                    "email" => ascendente ? query.OrderBy(u => u.Email) : query.OrderByDescending(u => u.Email),
                    "nome" => ascendente ? query.OrderBy(u => u.Nome).ThenBy(u => u.Sobrenome) : query.OrderByDescending(u => u.Nome).ThenByDescending(u => u.Sobrenome),
                    "datacriacao" => ascendente ? query.OrderBy(u => u.DataCriacao) : query.OrderByDescending(u => u.DataCriacao),
                    "ultimologin" => ascendente ? query.OrderBy(u => u.UltimoLogin) : query.OrderByDescending(u => u.UltimoLogin),
                    "ativo" => ascendente ? query.OrderBy(u => u.Ativo) : query.OrderByDescending(u => u.Ativo),
                    "totalpapeis" => ascendente ?
                        query.OrderBy(u => u.UsuarioPapeis.Count(up => up.Ativo)) :
                        query.OrderByDescending(u => u.UsuarioPapeis.Count(up => up.Ativo)),
                    "totalgrupos" => ascendente ?
                        query.OrderBy(u => u.UsuarioGrupos.Count(ug => ug.Ativo)) :
                        query.OrderByDescending(u => u.UsuarioGrupos.Count(ug => ug.Ativo)),
                    _ => query.OrderByDescending(u => u.DataCriacao)
                };
            }
            else
            {
                // Ordenações subsequentes
                queryOrdenada = criterio.Campo.ToLower() switch
                {
                    "email" => ascendente ? queryOrdenada!.ThenBy(u => u.Email) : queryOrdenada!.ThenByDescending(u => u.Email),
                    "nome" => ascendente ? queryOrdenada!.ThenBy(u => u.Nome).ThenBy(u => u.Sobrenome) : queryOrdenada!.ThenByDescending(u => u.Nome).ThenByDescending(u => u.Sobrenome),
                    "datacriacao" => ascendente ? queryOrdenada!.ThenBy(u => u.DataCriacao) : queryOrdenada!.ThenByDescending(u => u.DataCriacao),
                    "ultimologin" => ascendente ? queryOrdenada!.ThenBy(u => u.UltimoLogin) : queryOrdenada!.ThenByDescending(u => u.UltimoLogin),
                    "ativo" => ascendente ? queryOrdenada!.ThenBy(u => u.Ativo) : queryOrdenada!.ThenByDescending(u => u.Ativo),
                    _ => queryOrdenada
                };
            }
        }

        return queryOrdenada ?? query.OrderByDescending(u => u.DataCriacao);
    }

    /// <summary>
    /// Constrói estatísticas agregadas dos resultados
    /// </summary>
    private async Task<EstatisticasAgregadas> ConstruirEstatisticasAgregadas(IQueryable<Usuario> query)
    {
        var estatisticas = new EstatisticasAgregadas();

        // Executar queries em paralelo para performance
        var tarefas = new List<Task>
        {
            Task.Run(async () => estatisticas.TotalUsuarios = await query.CountAsync()),
            Task.Run(async () => estatisticas.UsuariosAtivos = await query.Where(u => u.Ativo).CountAsync()),
            Task.Run(async () => estatisticas.UsuariosInativos = await query.Where(u => !u.Ativo).CountAsync()),
            Task.Run(async () => estatisticas.UsuariosComEmail = await query.Where(u => u.EmailConfirmed).CountAsync()),
            Task.Run(async () => estatisticas.UsuariosComTelefone = await query.Where(u => u.PhoneNumberConfirmed).CountAsync()),
            Task.Run(async () => estatisticas.UsuariosSemUltimoLogin = await query.Where(u => u.UltimoLogin == null).CountAsync())
        };

        await Task.WhenAll(tarefas);

        // Estatísticas por papel
        estatisticas.DistribuicaoPorPapel = await query
            .SelectMany(u => u.UsuarioPapeis)
            .Where(up => up.Ativo)
            .GroupBy(up => up.Papel.Name)
            .Select(g => new EstatisticaCategoria
            {
                Categoria = g.Key!,
                Total = g.Count()
            })
            .OrderByDescending(e => e.Total)
            .Take(10)
            .ToListAsync();

        // Estatísticas por grupo
        estatisticas.DistribuicaoPorGrupo = await query
            .SelectMany(u => u.UsuarioGrupos)
            .Where(ug => ug.Ativo)
            .GroupBy(ug => ug.Grupo.Nome)
            .Select(g => new EstatisticaCategoria
            {
                Categoria = g.Key,
                Total = g.Count()
            })
            .OrderByDescending(e => e.Total)
            .Take(10)
            .ToListAsync();

        return estatisticas;
    }

    /// <summary>
    /// Obtém sugestões de email
    /// </summary>
    private async Task<List<SugestaoBusca>> ObterSugestoesEmail(string termo, int limite)
    {
        return await _context.Users
            .Where(u => u.Email!.ToLower().Contains(termo))
            .Select(u => new SugestaoBusca
            {
                Tipo = "email",
                Valor = u.Email!,
                Label = u.Email!,
                Detalhes = $"{u.Nome} {u.Sobrenome}"
            })
            .Distinct()
            .OrderBy(s => s.Valor.Length)
            .Take(limite)
            .ToListAsync();
    }

    /// <summary>
    /// Obtém sugestões de nome
    /// </summary>
    private async Task<List<SugestaoBusca>> ObterSugestoesNome(string termo, int limite)
    {
        return await _context.Users
            .Where(u => u.Nome.ToLower().Contains(termo) || u.Sobrenome.ToLower().Contains(termo))
            .Select(u => new SugestaoBusca
            {
                Tipo = "nome",
                Valor = u.Nome + " " + u.Sobrenome,
                Label = u.Nome + " " + u.Sobrenome,
                Detalhes = u.Email!
            })
            .Distinct()
            .OrderBy(s => s.Valor.Length)
            .Take(limite)
            .ToListAsync();
    }

    /// <summary>
    /// Obtém sugestões de papel
    /// </summary>
    private async Task<List<SugestaoBusca>> ObterSugestoesPapel(string termo, int limite)
    {
        return await _roleManager.Roles
            .Where(r => r.Name!.ToLower().Contains(termo) && r.Ativo)
            .Select(r => new SugestaoBusca
            {
                Tipo = "papel",
                Valor = r.Name!,
                Label = r.Name!,
                Detalhes = r.Descricao
            })
            .OrderBy(s => s.Valor.Length)
            .Take(limite)
            .ToListAsync();
    }

    /// <summary>
    /// Obtém sugestões de grupo
    /// </summary>
    private async Task<List<SugestaoBusca>> ObterSugestoesGrupo(string termo, int limite)
    {
        return await _context.Grupos
            .Where(g => g.Nome.ToLower().Contains(termo) && g.Ativo)
            .Select(g => new SugestaoBusca
            {
                Tipo = "grupo",
                Valor = g.Nome,
                Label = g.Nome,
                Detalhes = g.Descricao
            })
            .OrderBy(s => s.Valor.Length)
            .Take(limite)
            .ToListAsync();
    }

    #endregion

    #region Métodos Auxiliares de Operações em Lote

    /// <summary>
    /// Verifica permissão específica baseada no tipo de operação
    /// </summary>
    private string? VerificarPermissaoEspecificaLote(string tipoOperacao)
    {
        return tipoOperacao.ToLower() switch
        {
            "criar" => "Usuarios.Criar",
            "atualizar" => "Usuarios.Editar",
            "ativar" or "desativar" => "Usuarios.Editar",
            "excluir" => "Usuarios.Excluir",
            "atribuir-papeis" or "remover-papeis" or "limpar-papeis" => "Usuarios.GerenciarPapeis",
            "exportar" => "Usuarios.Exportar",
            "reset-senha" => "Usuarios.ResetarSenha",
            "confirmar-email" or "confirmar-telefone" => "Usuarios.Confirmar",
            _ => null
        };
    }

    /// <summary>
    /// Determina se a operação deve ser executada de forma assíncrona
    /// </summary>
    private bool DeveSerExecutadoAssincrono(SolicitacaoOperacaoLote request)
    {
        // ✅ CRITÉRIOS PARA EXECUÇÃO ASSÍNCRONA
        var totalItens = (request.UsuariosIds?.Count ?? 0) + (request.DadosUsuarios?.Count ?? 0);

        return totalItens > 100 || // Mais de 100 itens
               request.TipoOperacao.ToLower() == "exportar" || // Exportação sempre assíncrona
               request.NotificarConclusao; // Se pediu notificação
    }

    /// <summary>
    /// Executa a operação em lote de forma síncrona ou assíncrona
    /// </summary>
    private async Task<RespostaOperacaoLote> ExecutarOperacaoLoteAsync(
        SolicitacaoOperacaoLote request,
        int usuarioLogadoId,
        string? jobId = null)
    {
        var inicio = DateTime.UtcNow;
        var resposta = new RespostaOperacaoLote
        {
            TipoOperacao = request.TipoOperacao,
            JobId = jobId ?? Guid.NewGuid().ToString(),
            IniciadoEm = inicio
        };

        try
        {
            switch (request.TipoOperacao.ToLower())
            {
                case "criar":
                    await ExecutarCriacaoLote(request.DadosUsuarios!, resposta, usuarioLogadoId);
                    break;

                case "atualizar":
                    await ExecutarAtualizacaoLote(request.DadosUsuarios!, resposta, usuarioLogadoId);
                    break;

                case "ativar":
                    await ExecutarAtivacaoLote(request.UsuariosIds!, true, resposta, usuarioLogadoId);
                    break;

                case "desativar":
                    await ExecutarAtivacaoLote(request.UsuariosIds!, false, resposta, usuarioLogadoId);
                    break;

                case "excluir":
                    await ExecutarExclusaoLote(request.UsuariosIds!, resposta, usuarioLogadoId);
                    break;

                case "atribuir-papeis":
                    await ExecutarGerenciamentoPapeisLote(request.UsuariosIds!,
                        request.ParametrosOperacao!["papeis"], "adicionar", resposta, usuarioLogadoId);
                    break;

                case "remover-papeis":
                    await ExecutarGerenciamentoPapeisLote(request.UsuariosIds!,
                        request.ParametrosOperacao!["papeis"], "remover", resposta, usuarioLogadoId);
                    break;

                case "limpar-papeis":
                    await ExecutarGerenciamentoPapeisLote(request.UsuariosIds!,
                        "", "limpar", resposta, usuarioLogadoId);
                    break;

                case "exportar":
                    await ExecutarExportacaoLote(request.UsuariosIds,
                        request.ParametrosOperacao!, resposta, usuarioLogadoId);
                    break;

                case "reset-senha":
                    await ExecutarResetSenhaLote(request.UsuariosIds!, resposta, usuarioLogadoId);
                    break;

                case "confirmar-email":
                    await ExecutarConfirmacaoLote(request.UsuariosIds!, "email", resposta, usuarioLogadoId);
                    break;

                case "confirmar-telefone":
                    await ExecutarConfirmacaoLote(request.UsuariosIds!, "telefone", resposta, usuarioLogadoId);
                    break;

                default:
                    throw new NotSupportedException($"Operação não suportada: {request.TipoOperacao}");
            }

            resposta.ConcluidoEm = DateTime.UtcNow;
            resposta.Sucesso = resposta.TotalErros == 0 || resposta.TotalSucessos > 0;
            resposta.Mensagem = resposta.Sucesso ?
                $"Operação concluída: {resposta.TotalSucessos} sucessos, {resposta.TotalErros} erros" :
                "Operação falhou com múltiplos erros";

            // ✅ REGISTRAR AUDITORIA DA OPERAÇÃO COMPLETA
            await RegistrarAuditoria("Usuarios.OperacaoLote", "Usuarios", null,
                $"Operação em lote '{request.TipoOperacao}' concluída",
                dadosAntes: request,
                dadosDepois: new {
                    TotalProcessados = resposta.TotalProcessados,
                    TotalSucessos = resposta.TotalSucessos,
                    TotalErros = resposta.TotalErros,
                    TempoExecucao = resposta.TempoExecucao.TotalMilliseconds
                });

            return resposta;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro na execução da operação em lote: {TipoOperacao}", request.TipoOperacao);

            resposta.ConcluidoEm = DateTime.UtcNow;
            resposta.Sucesso = false;
            resposta.Mensagem = $"Erro na operação: {ex.Message}";

            return resposta;
        }
    }

    /// <summary>
    /// Executa criação de usuários em lote
    /// </summary>
    private async Task ExecutarCriacaoLote(List<DadosUsuarioLote> dados, RespostaOperacaoLote resposta, int usuarioLogadoId)
    {
        foreach (var dadosUsuario in dados)
        {
            var item = new ItemProcessado
            {
                Id = Guid.NewGuid().ToString(),
                Identificador = dadosUsuario.Email ?? "N/A"
            };

            try
            {
                // ✅ CRIAR USUÁRIO INDIVIDUAL
                var novoUsuario = new Usuario
                {
                    UserName = dadosUsuario.Email,
                    Email = dadosUsuario.Email,
                    Nome = dadosUsuario.Nome!,
                    Sobrenome = dadosUsuario.Sobrenome!,
                    NomeCompleto = $"{dadosUsuario.Nome} {dadosUsuario.Sobrenome}",
                    PhoneNumber = dadosUsuario.Telefone,
                    Ativo = dadosUsuario.Ativo ?? true,
                    EmailConfirmed = dadosUsuario.EmailConfirmado ?? false,
                    PhoneNumberConfirmed = dadosUsuario.TelefoneConfirmado ?? false,
                    Observacoes = dadosUsuario.Observacoes
                };

                var resultado = await _userManager.CreateAsync(novoUsuario, dadosUsuario.Senha!);

                if (resultado.Succeeded)
                {
                    // ✅ ATRIBUIR PAPÉIS SE ESPECIFICADOS
                    if (dadosUsuario.Papeis?.Any() == true)
                    {
                        await _userManager.AddToRolesAsync(novoUsuario, dadosUsuario.Papeis);
                    }

                    item.Status = "Sucesso";
                    item.Mensagem = $"Usuário criado com sucesso (ID: {novoUsuario.Id})";
                    resposta.TotalSucessos++;
                }
                else
                {
                    item.Status = "Erro";
                    item.Mensagem = string.Join("; ", resultado.Errors.Select(e => e.Description));
                    resposta.TotalErros++;
                }
            }
            catch (Exception ex)
            {
                item.Status = "Erro";
                item.Mensagem = ex.Message;
                resposta.TotalErros++;
            }

            resposta.ItensProcessados.Add(item);
            resposta.TotalProcessados++;
        }
    }

    /// <summary>
    /// Executa ativação/desativação de usuários em lote
    /// </summary>
    private async Task ExecutarAtivacaoLote(List<int> usuariosIds, bool ativar, RespostaOperacaoLote resposta, int usuarioLogadoId)
    {
        foreach (var usuarioId in usuariosIds)
        {
            var item = new ItemProcessado
            {
                Id = usuarioId.ToString(),
                Identificador = usuarioId.ToString()
            };

            try
            {
                var usuario = await _userManager.FindByIdAsync(usuarioId.ToString());
                if (usuario == null)
                {
                    item.Status = "Erro";
                    item.Mensagem = "Usuário não encontrado";
                    resposta.TotalErros++;
                    continue;
                }

                // ✅ VALIDAÇÃO: não pode desativar próprio usuário
                if (!ativar && usuario.Id == usuarioLogadoId)
                {
                    item.Status = "Ignorado";
                    item.Mensagem = "Não é possível desativar seu próprio usuário";
                    resposta.TotalIgnorados++;
                    continue;
                }

                usuario.Ativo = ativar;
                usuario.DataAtualizacao = DateTime.UtcNow;

                var resultado = await _userManager.UpdateAsync(usuario);

                if (resultado.Succeeded)
                {
                    item.Status = "Sucesso";
                    item.Mensagem = $"Usuário {(ativar ? "ativado" : "desativado")} com sucesso";
                    item.Identificador = usuario.Email!;
                    resposta.TotalSucessos++;
                }
                else
                {
                    item.Status = "Erro";
                    item.Mensagem = string.Join("; ", resultado.Errors.Select(e => e.Description));
                    resposta.TotalErros++;
                }
            }
            catch (Exception ex)
            {
                item.Status = "Erro";
                item.Mensagem = ex.Message;
                resposta.TotalErros++;
            }

            resposta.ItensProcessados.Add(item);
            resposta.TotalProcessados++;
        }
    }

    /// <summary>
    /// Executa exportação de usuários em lote
    /// </summary>
    private async Task ExecutarExportacaoLote(List<int>? usuariosIds, Dictionary<string, string> parametros, RespostaOperacaoLote resposta, int usuarioLogadoId)
    {
        try
        {
            var formato = parametros["formato"].ToLower();

            // ✅ BUSCAR USUÁRIOS PARA EXPORTAÇÃO
            IQueryable<Usuario> query = _context.Users.Include(u => u.UsuarioPapeis)
                                                      .ThenInclude(up => up.Papel);

            if (usuariosIds?.Any() == true)
            {
                query = query.Where(u => usuariosIds.Contains(u.Id));
            }
            else if (parametros.ContainsKey("todos"))
            {
                // Exportar todos - aplicar filtros básicos se necessário
                query = query.Where(u => u.Ativo); // Apenas usuários ativos por padrão
            }

            var usuarios = await query.ToListAsync();

            // ✅ GERAR ARQUIVO DE EXPORTAÇÃO
            var conteudo = formato switch
            {
                "csv" => GerarCsv(usuarios),
                "json" => GerarJson(usuarios),
                "xlsx" => GerarXlsx(usuarios),
                _ => throw new NotSupportedException($"Formato não suportado: {formato}")
            };

            resposta.ArquivoExportacao = new ArquivoExportacao
            {
                NomeArquivo = $"usuarios_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{formato}",
                TipoConteudo = ObterContentType(formato),
                ConteudoBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(conteudo)),
                Tamanho = conteudo.Length
            };

            resposta.TotalProcessados = usuarios.Count;
            resposta.TotalSucessos = usuarios.Count;
            resposta.Mensagem = $"Exportação concluída: {usuarios.Count} usuários";
        }
        catch (Exception ex)
        {
            resposta.TotalErros++;
            resposta.ItensProcessados.Add(new ItemProcessado
            {
                Id = "exportacao",
                Status = "Erro",
                Mensagem = ex.Message
            });
        }
    }

    /// <summary>
    /// Gera conteúdo CSV dos usuários
    /// </summary>
    private string GerarCsv(List<Usuario> usuarios)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Id,Email,Nome,Sobrenome,Ativo,DataCriacao,UltimoLogin,Papeis");

        foreach (var usuario in usuarios)
        {
            var papeis = string.Join(";", usuario.UsuarioPapeis
                .Where(up => up.Ativo)
                .Select(up => up.Papel.Name));

            sb.AppendLine($"{usuario.Id},{usuario.Email},{usuario.Nome},{usuario.Sobrenome}," +
                         $"{usuario.Ativo},{usuario.DataCriacao:yyyy-MM-dd},{usuario.UltimoLogin:yyyy-MM-dd}," +
                         $"\"{papeis}\"");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Gera conteúdo JSON dos usuários
    /// </summary>
    private string GerarJson(List<Usuario> usuarios)
    {
        var dados = usuarios.Select(u => new
        {
            u.Id,
            u.Email,
            u.Nome,
            u.Sobrenome,
            u.Ativo,
            u.DataCriacao,
            u.UltimoLogin,
            Papeis = u.UsuarioPapeis.Where(up => up.Ativo).Select(up => up.Papel.Name).ToList()
        });

        return JsonSerializer.Serialize(dados, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Gera conteúdo XLSX dos usuários (mock - implementar com EPPlus se necessário)
    /// </summary>
    private string GerarXlsx(List<Usuario> usuarios)
    {
        // ✅ MOCK - implementar com EPPlus posteriormente
        return GerarCsv(usuarios); // Por enquanto retorna CSV
    }

    /// <summary>
    /// Obtém Content-Type baseado no formato
    /// </summary>
    private string ObterContentType(string formato)
    {
        return formato.ToLower() switch
        {
            "csv" => "text/csv",
            "json" => "application/json",
            "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "xml" => "application/xml",
            _ => "application/octet-stream"
        };
    }

    /// <summary>
    /// Ativa usuários em lote
    /// </summary>
    private async Task<RespostaOperacaoLote> ExecutarLoteAtivar(SolicitacaoOperacaoLote request, int usuarioLogadoId)
    {
        var resultado = new RespostaOperacaoLote
        {
            TipoOperacao = "ativar",
            JobId = Guid.NewGuid().ToString(),
            IniciadoEm = DateTime.UtcNow
        };

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var usuarios = await _context.Users
                .Where(u => request.UsuariosIds!.Contains(u.Id))
                .ToListAsync();

            resultado.TotalProcessados = usuarios.Count;

            foreach (var usuario in usuarios)
            {
                try
                {
                    if (!usuario.Ativo)
                    {
                        usuario.Ativo = true;
                        usuario.DataAtualizacao = DateTime.UtcNow;

                        var resultadoUpdate = await _userManager.UpdateAsync(usuario);
                        if (resultadoUpdate.Succeeded)
                        {
                            resultado.ItensProcessados.Add(new ItemProcessado
                            {
                                Id = usuario.Id.ToString(),
                                Identificador = usuario.Email!,
                                Status = "Sucesso",
                                Mensagem = "Usuário ativado com sucesso"
                            });
                            resultado.TotalSucessos++;
                        }
                        else
                        {
                            resultado.ItensProcessados.Add(new ItemProcessado
                            {
                                Id = usuario.Id.ToString(),
                                Identificador = usuario.Email!,
                                Status = "Erro",
                                Mensagem = $"Erro ao ativar: {string.Join(", ", resultadoUpdate.Errors.Select(e => e.Description))}"
                            });
                            resultado.TotalErros++;
                        }
                    }
                    else
                    {
                        resultado.ItensProcessados.Add(new ItemProcessado
                        {
                            Id = usuario.Id.ToString(),
                            Identificador = usuario.Email!,
                            Status = "Ignorado",
                            Mensagem = "Usuário já estava ativo"
                        });
                        resultado.TotalIgnorados++;
                    }
                }
                catch (Exception ex)
                {
                    resultado.ItensProcessados.Add(new ItemProcessado
                    {
                        Id = usuario.Id.ToString(),
                        Identificador = usuario.Email!,
                        Status = "Erro",
                        Mensagem = $"Erro interno: {ex.Message}"
                    });
                    resultado.TotalErros++;
                }
            }

            await transaction.CommitAsync();
            resultado.ConcluidoEm = DateTime.UtcNow;
            resultado.Sucesso = resultado.TotalErros == 0;
            resultado.Mensagem = $"Operação concluída: {resultado.TotalSucessos} sucessos, {resultado.TotalErros} erros, {resultado.TotalIgnorados} ignorados";

            return resultado;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Desativa usuários em lote
    /// </summary>
    private async Task<RespostaOperacaoLote> ExecutarLoteDesativar(SolicitacaoOperacaoLote request, int usuarioLogadoId)
    {
        var resultado = new RespostaOperacaoLote
        {
            TipoOperacao = "desativar",
            JobId = Guid.NewGuid().ToString(),
            IniciadoEm = DateTime.UtcNow
        };

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var usuarios = await _context.Users
                .Include(u => u.UsuarioPapeis)
                    .ThenInclude(up => up.Papel)
                .Where(u => request.UsuariosIds!.Contains(u.Id))
                .ToListAsync();

            resultado.TotalProcessados = usuarios.Count;

            foreach (var usuario in usuarios)
            {
                try
                {
                    // ✅ VALIDAÇÕES DE SEGURANÇA
                    if (usuario.Id == usuarioLogadoId)
                    {
                        resultado.ItensProcessados.Add(new ItemProcessado
                        {
                            Id = usuario.Id.ToString(),
                            Identificador = usuario.Email!,
                            Status = "Erro",
                            Mensagem = "Não é possível desativar seu próprio usuário"
                        });
                        resultado.TotalErros++;
                        continue;
                    }

                    // ✅ VERIFICAR SE É ÚLTIMO SUPER ADMIN
                    var ehSuperAdmin = usuario.UsuarioPapeis.Any(up => up.Papel.Name == "SuperAdmin" && up.Ativo);
                    if (ehSuperAdmin)
                    {
                        var totalSuperAdmins = await _context.Users
                            .Where(u => u.UsuarioPapeis.Any(up => up.Papel.Name == "SuperAdmin" && up.Ativo) && u.Ativo)
                            .CountAsync();

                        if (totalSuperAdmins <= 1)
                        {
                            resultado.ItensProcessados.Add(new ItemProcessado
                            {
                                Id = usuario.Id.ToString(),
                                Identificador = usuario.Email!,
                                Status = "Erro",
                                Mensagem = "Não é possível desativar o último Super Administrador"
                            });
                            resultado.TotalErros++;
                            continue;
                        }
                    }

                    if (usuario.Ativo)
                    {
                        usuario.Ativo = false;
                        usuario.DataAtualizacao = DateTime.UtcNow;

                        // ✅ DESATIVAR RELACIONAMENTOS
                        foreach (var usuarioPapel in usuario.UsuarioPapeis.Where(up => up.Ativo))
                        {
                            usuarioPapel.Ativo = false;
                        }

                        var resultadoUpdate = await _userManager.UpdateAsync(usuario);
                        if (resultadoUpdate.Succeeded)
                        {
                            resultado.ItensProcessados.Add(new ItemProcessado
                            {
                                Id = usuario.Id.ToString(),
                                Identificador = usuario.Email!,
                                Status = "Sucesso",
                                Mensagem = "Usuário desativado com sucesso"
                            });
                            resultado.TotalSucessos++;
                        }
                        else
                        {
                            resultado.ItensProcessados.Add(new ItemProcessado
                            {
                                Id = usuario.Id.ToString(),
                                Identificador = usuario.Email!,
                                Status = "Erro",
                                Mensagem = $"Erro ao desativar: {string.Join(", ", resultadoUpdate.Errors.Select(e => e.Description))}"
                            });
                            resultado.TotalErros++;
                        }
                    }
                    else
                    {
                        resultado.ItensProcessados.Add(new ItemProcessado
                        {
                            Id = usuario.Id.ToString(),
                            Identificador = usuario.Email!,
                            Status = "Ignorado",
                            Mensagem = "Usuário já estava inativo"
                        });
                        resultado.TotalIgnorados++;
                    }
                }
                catch (Exception ex)
                {
                    resultado.ItensProcessados.Add(new ItemProcessado
                    {
                        Id = usuario.Id.ToString(),
                        Identificador = usuario.Email!,
                        Status = "Erro",
                        Mensagem = $"Erro interno: {ex.Message}"
                    });
                    resultado.TotalErros++;
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            resultado.ConcluidoEm = DateTime.UtcNow;
            resultado.Sucesso = resultado.TotalErros == 0;
            resultado.Mensagem = $"Operação concluída: {resultado.TotalSucessos} sucessos, {resultado.TotalErros} erros, {resultado.TotalIgnorados} ignorados";

            return resultado;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Exclui usuários em lote (soft delete)
    /// </summary>
    private async Task<RespostaOperacaoLote> ExecutarLoteExcluir(SolicitacaoOperacaoLote request, int usuarioLogadoId)
    {
        var resultado = new RespostaOperacaoLote
        {
            TipoOperacao = "excluir",
            JobId = Guid.NewGuid().ToString(),
            IniciadoEm = DateTime.UtcNow
        };

        var exclusaoPermanente = request.ParametrosOperacao?.GetValueOrDefault("exclusaoPermanente", "false") == "true";

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var usuarios = await _context.Users
                .Include(u => u.UsuarioPapeis)
                    .ThenInclude(up => up.Papel)
                .Where(u => request.UsuariosIds!.Contains(u.Id))
                .ToListAsync();

            resultado.TotalProcessados = usuarios.Count;

            foreach (var usuario in usuarios)
            {
                try
                {
                    // ✅ VALIDAR REMOÇÃO
                    var validacao = await ValidarRemocaoUsuario(usuario, usuarioLogadoId, exclusaoPermanente);
                    if (validacao != null)
                    {
                        resultado.ItensProcessados.Add(new ItemProcessado
                        {
                            Id = usuario.Id.ToString(),
                            Identificador = usuario.Email!,
                            Status = "Erro",
                            Mensagem = "Usuário não pode ser removido (validação de segurança)"
                        });
                        resultado.TotalErros++;
                        continue;
                    }

                    if (exclusaoPermanente)
                    {
                        await RealizarExclusaoPermanente(usuario);
                        resultado.ItensProcessados.Add(new ItemProcessado
                        {
                            Id = usuario.Id.ToString(),
                            Identificador = usuario.Email!,
                            Status = "Sucesso",
                            Mensagem = "Usuário excluído permanentemente"
                        });
                    }
                    else
                    {
                        await RealizarDesativacao(usuario);
                        resultado.ItensProcessados.Add(new ItemProcessado
                        {
                            Id = usuario.Id.ToString(),
                            Identificador = usuario.Email!,
                            Status = "Sucesso",
                            Mensagem = "Usuário desativado (soft delete)"
                        });
                    }

                    resultado.TotalSucessos++;
                }
                catch (Exception ex)
                {
                    resultado.ItensProcessados.Add(new ItemProcessado
                    {
                        Id = usuario.Id.ToString(),
                        Identificador = usuario.Email!,
                        Status = "Erro",
                        Mensagem = $"Erro interno: {ex.Message}"
                    });
                    resultado.TotalErros++;
                }
            }

            await transaction.CommitAsync();

            resultado.ConcluidoEm = DateTime.UtcNow;
            resultado.Sucesso = resultado.TotalErros == 0;
            resultado.Mensagem = $"Operação concluída: {resultado.TotalSucessos} sucessos, {resultado.TotalErros} erros";

            return resultado;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Atribui papéis em lote
    /// </summary>
    private async Task<RespostaOperacaoLote> ExecutarLoteAtribuirPapeis(SolicitacaoOperacaoLote request, int usuarioLogadoId)
    {
        var resultado = new RespostaOperacaoLote
        {
            TipoOperacao = "atribuir-papeis",
            JobId = Guid.NewGuid().ToString(),
            IniciadoEm = DateTime.UtcNow
        };

        var papeisParaAtribuir = request.ParametrosOperacao?.GetValueOrDefault("papeis", "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .ToList() ?? new List<string>();

        if (!papeisParaAtribuir.Any())
        {
            resultado.Sucesso = false;
            resultado.Mensagem = "Nenhum papel especificado para atribuição";
            resultado.ConcluidoEm = DateTime.UtcNow;
            return resultado;
        }

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var usuarios = await _context.Users
                .Where(u => request.UsuariosIds!.Contains(u.Id) && u.Ativo)
                .ToListAsync();

            resultado.TotalProcessados = usuarios.Count;

            foreach (var usuario in usuarios)
            {
                try
                {
                    var papeisAtuais = await _userManager.GetRolesAsync(usuario);
                    var papeisNovos = papeisParaAtribuir.Except(papeisAtuais).ToList();

                    if (papeisNovos.Any())
                    {
                        var resultadoAtribuicao = await _userManager.AddToRolesAsync(usuario, papeisNovos);
                        if (resultadoAtribuicao.Succeeded)
                        {
                            // ✅ ATUALIZAR METADADOS
                            await AtualizarMetadadosPapeis(usuario.Id, papeisNovos, usuarioLogadoId);

                            resultado.ItensProcessados.Add(new ItemProcessado
                            {
                                Id = usuario.Id.ToString(),
                                Identificador = usuario.Email!,
                                Status = "Sucesso",
                                Mensagem = $"Papéis atribuídos: {string.Join(", ", papeisNovos)}"
                            });
                            resultado.TotalSucessos++;
                        }
                        else
                        {
                            resultado.ItensProcessados.Add(new ItemProcessado
                            {
                                Id = usuario.Id.ToString(),
                                Identificador = usuario.Email!,
                                Status = "Erro",
                                Mensagem = $"Erro ao atribuir papéis: {string.Join(", ", resultadoAtribuicao.Errors.Select(e => e.Description))}"
                            });
                            resultado.TotalErros++;
                        }
                    }
                    else
                    {
                        resultado.ItensProcessados.Add(new ItemProcessado
                        {
                            Id = usuario.Id.ToString(),
                            Identificador = usuario.Email!,
                            Status = "Ignorado",
                            Mensagem = "Usuário já possui todos os papéis especificados"
                        });
                        resultado.TotalIgnorados++;
                    }
                }
                catch (Exception ex)
                {
                    resultado.ItensProcessados.Add(new ItemProcessado
                    {
                        Id = usuario.Id.ToString(),
                        Identificador = usuario.Email!,
                        Status = "Erro",
                        Mensagem = $"Erro interno: {ex.Message}"
                    });
                    resultado.TotalErros++;
                }
            }

            await transaction.CommitAsync();

            resultado.ConcluidoEm = DateTime.UtcNow;
            resultado.Sucesso = resultado.TotalErros == 0;
            resultado.Mensagem = $"Operação concluída: {resultado.TotalSucessos} sucessos, {resultado.TotalErros} erros, {resultado.TotalIgnorados} ignorados";

            return resultado;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Remove papéis em lote
    /// </summary>
    private async Task<RespostaOperacaoLote> ExecutarLoteRemoverPapeis(SolicitacaoOperacaoLote request, int usuarioLogadoId)
    {
        var resultado = new RespostaOperacaoLote
        {
            TipoOperacao = "remover-papeis",
            JobId = Guid.NewGuid().ToString(),
            IniciadoEm = DateTime.UtcNow
        };

        var papeisParaRemover = request.ParametrosOperacao?.GetValueOrDefault("papeis", "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .ToList() ?? new List<string>();

        if (!papeisParaRemover.Any())
        {
            resultado.Sucesso = false;
            resultado.Mensagem = "Nenhum papel especificado para remoção";
            resultado.ConcluidoEm = DateTime.UtcNow;
            return resultado;
        }

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var usuarios = await _context.Users
                .Include(u => u.UsuarioPapeis)
                    .ThenInclude(up => up.Papel)
                .Where(u => request.UsuariosIds!.Contains(u.Id))
                .ToListAsync();

            resultado.TotalProcessados = usuarios.Count;

            foreach (var usuario in usuarios)
            {
                try
                {
                    // ✅ VALIDAR REMOÇÃO DE SUPER ADMIN
                    if (usuario.Id == usuarioLogadoId && papeisParaRemover.Contains("SuperAdmin"))
                    {
                        var totalSuperAdmins = await _context.Users
                            .Where(u => u.UsuarioPapeis.Any(up => up.Papel.Name == "SuperAdmin" && up.Ativo) && u.Ativo)
                            .CountAsync();

                        if (totalSuperAdmins <= 1)
                        {
                            resultado.ItensProcessados.Add(new ItemProcessado
                            {
                                Id = usuario.Id.ToString(),
                                Identificador = usuario.Email!,
                                Status = "Erro",
                                Mensagem = "Não é possível remover o papel SuperAdmin do último Super Administrador"
                            });
                            resultado.TotalErros++;
                            continue;
                        }
                    }

                    var papeisAtuais = await _userManager.GetRolesAsync(usuario);
                    var papeisExistentes = papeisParaRemover.Intersect(papeisAtuais).ToList();

                    if (papeisExistentes.Any())
                    {
                        var resultadoRemocao = await _userManager.RemoveFromRolesAsync(usuario, papeisExistentes);
                        if (resultadoRemocao.Succeeded)
                        {
                            resultado.ItensProcessados.Add(new ItemProcessado
                            {
                                Id = usuario.Id.ToString(),
                                Identificador = usuario.Email!,
                                Status = "Sucesso",
                                Mensagem = $"Papéis removidos: {string.Join(", ", papeisExistentes)}"
                            });
                            resultado.TotalSucessos++;
                        }
                        else
                        {
                            resultado.ItensProcessados.Add(new ItemProcessado
                            {
                                Id = usuario.Id.ToString(),
                                Identificador = usuario.Email!,
                                Status = "Erro",
                                Mensagem = $"Erro ao remover papéis: {string.Join(", ", resultadoRemocao.Errors.Select(e => e.Description))}"
                            });
                            resultado.TotalErros++;
                        }
                    }
                    else
                    {
                        resultado.ItensProcessados.Add(new ItemProcessado
                        {
                            Id = usuario.Id.ToString(),
                            Identificador = usuario.Email!,
                            Status = "Ignorado",
                            Mensagem = "Usuário não possui os papéis especificados"
                        });
                        resultado.TotalIgnorados++;
                    }
                }
                catch (Exception ex)
                {
                    resultado.ItensProcessados.Add(new ItemProcessado
                    {
                        Id = usuario.Id.ToString(),
                        Identificador = usuario.Email!,
                        Status = "Erro",
                        Mensagem = $"Erro interno: {ex.Message}"
                    });
                    resultado.TotalErros++;
                }
            }

            await transaction.CommitAsync();

            resultado.ConcluidoEm = DateTime.UtcNow;
            resultado.Sucesso = resultado.TotalErros == 0;
            resultado.Mensagem = $"Operação concluída: {resultado.TotalSucessos} sucessos, {resultado.TotalErros} erros, {resultado.TotalIgnorados} ignorados";

            return resultado;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Exporta usuários em lote
    /// </summary>
    private async Task<RespostaOperacaoLote> ExecutarLoteExportar(SolicitacaoOperacaoLote request, int usuarioLogadoId)
    {
        var resultado = new RespostaOperacaoLote
        {
            TipoOperacao = "exportar",
            JobId = Guid.NewGuid().ToString(),
            IniciadoEm = DateTime.UtcNow
        };

        try
        {
            var formato = request.ParametrosOperacao?.GetValueOrDefault("formato", "json")?.ToLower() ?? "json";
            var incluirInativos = request.ParametrosOperacao?.GetValueOrDefault("incluirInativos", "false") == "true";

            var query = _context.Users
                .Include(u => u.UsuarioPapeis.Where(up => up.Ativo))
                    .ThenInclude(up => up.Papel)
                .AsQueryable();

            if (request.UsuariosIds?.Any() == true)
            {
                query = query.Where(u => request.UsuariosIds.Contains(u.Id));
            }

            if (!incluirInativos)
            {
                query = query.Where(u => u.Ativo);
            }

            var usuarios = await query.ToListAsync();
            resultado.TotalProcessados = usuarios.Count;

            var dadosExportacao = usuarios.Select(u => new
            {
                Id = u.Id,
                Email = u.Email,
                Nome = u.Nome,
                Sobrenome = u.Sobrenome,
                NomeCompleto = u.NomeCompleto,
                Telefone = u.PhoneNumber,
                Ativo = u.Ativo,
                EmailConfirmado = u.EmailConfirmed,
                TelefoneConfirmado = u.PhoneNumberConfirmed,
                DataCriacao = u.DataCriacao,
                DataAtualizacao = u.DataAtualizacao,
                UltimoLogin = u.UltimoLogin,
                Papeis = u.UsuarioPapeis.Where(up => up.Ativo).Select(up => up.Papel.Name).ToList(),
                Observacoes = u.Observacoes
            }).ToList();

            string conteudoExportacao;
            string nomeArquivo;
            string contentType;

            switch (formato)
            {
                case "csv":
                    conteudoExportacao = GerarExportacaoCSV(dadosExportacao);
                    nomeArquivo = $"usuarios_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
                    contentType = "text/csv";
                    break;

                case "excel":
                    // ✅ Para implementação futura com EPPlus ou similar
                    conteudoExportacao = "Formato Excel não implementado ainda";
                    nomeArquivo = $"usuarios_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
                    contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    break;

                case "json":
                default:
                    conteudoExportacao = System.Text.Json.JsonSerializer.Serialize(dadosExportacao, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    nomeArquivo = $"usuarios_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
                    contentType = "application/json";
                    break;
            }

            resultado.TotalSucessos = usuarios.Count;
            resultado.ConcluidoEm = DateTime.UtcNow;
            resultado.Sucesso = true;
            resultado.Mensagem = $"Exportação concluída: {usuarios.Count} usuários exportados";
            resultado.ArquivoExportacao = new ArquivoExportacao
            {
                NomeArquivo = nomeArquivo,
                TipoConteudo = contentType,
                ConteudoBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(conteudoExportacao)),
                Tamanho = System.Text.Encoding.UTF8.GetBytes(conteudoExportacao).Length
            };

            return resultado;
        }
        catch (Exception ex)
        {
            resultado.ConcluidoEm = DateTime.UtcNow;
            resultado.Sucesso = false;
            resultado.TotalErros = 1;
            resultado.Mensagem = $"Erro na exportação: {ex.Message}";
            return resultado;
        }
    }

    /// <summary>
    /// Cria usuários em lote
    /// </summary>
    private async Task<RespostaOperacaoLote> ExecutarLoteCriar(SolicitacaoOperacaoLote request, int usuarioLogadoId)
    {
        var resultado = new RespostaOperacaoLote
        {
            TipoOperacao = "criar",
            JobId = Guid.NewGuid().ToString(),
            IniciadoEm = DateTime.UtcNow
        };

        if (request.DadosUsuarios?.Any() != true)
        {
            resultado.Sucesso = false;
            resultado.Mensagem = "Nenhum dado de usuário fornecido para criação";
            resultado.ConcluidoEm = DateTime.UtcNow;
            return resultado;
        }

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            resultado.TotalProcessados = request.DadosUsuarios.Count;

            foreach (var dadosUsuario in request.DadosUsuarios)
            {
                try
                {
                    // ✅ VALIDAR DADOS BÁSICOS
                    if (string.IsNullOrEmpty(dadosUsuario.Email) || string.IsNullOrEmpty(dadosUsuario.Nome))
                    {
                        resultado.ItensProcessados.Add(new ItemProcessado
                        {
                            Id = "N/A",
                            Identificador = dadosUsuario.Email ?? "Email não fornecido",
                            Status = "Erro",
                            Mensagem = "Email e Nome são obrigatórios"
                        });
                        resultado.TotalErros++;
                        continue;
                    }

                    // ✅ VERIFICAR SE JÁ EXISTE
                    var usuarioExistente = await _userManager.FindByEmailAsync(dadosUsuario.Email);
                    if (usuarioExistente != null)
                    {
                        resultado.ItensProcessados.Add(new ItemProcessado
                        {
                            Id = usuarioExistente.Id.ToString(),
                            Identificador = dadosUsuario.Email,
                            Status = "Erro",
                            Mensagem = "Usuário já existe com este email"
                        });
                        resultado.TotalErros++;
                        continue;
                    }

                    var novoUsuario = new Usuario
                    {
                        UserName = dadosUsuario.Email,
                        Email = dadosUsuario.Email,
                        EmailConfirmed = dadosUsuario.EmailConfirmado ?? true,
                        Nome = dadosUsuario.Nome,
                        Sobrenome = dadosUsuario.Sobrenome ?? "",
                        NomeCompleto = $"{dadosUsuario.Nome} {dadosUsuario.Sobrenome}",
                        PhoneNumber = dadosUsuario.Telefone,
                        PhoneNumberConfirmed = dadosUsuario.TelefoneConfirmado ?? false,
                        Ativo = dadosUsuario.Ativo ?? true,
                        Observacoes = dadosUsuario.Observacoes,
                        DataCriacao = DateTime.UtcNow
                    };

                    var senha = dadosUsuario.Senha ?? GerarSenhaAleatoria();
                    var resultadoCriacao = await _userManager.CreateAsync(novoUsuario, senha);

                    if (resultadoCriacao.Succeeded)
                    {
                        // ✅ ATRIBUIR PAPÉIS SE ESPECIFICADOS
                        if (dadosUsuario.Papeis?.Any() == true)
                        {
                            var papeisValidos = await _roleManager.Roles
                                .Where(r => dadosUsuario.Papeis.Contains(r.Name!) && r.Ativo)
                                .Select(r => r.Name!)
                                .ToListAsync();

                            if (papeisValidos.Any())
                            {
                                await _userManager.AddToRolesAsync(novoUsuario, papeisValidos);
                                await AtualizarMetadadosPapeis(novoUsuario.Id, papeisValidos, usuarioLogadoId);
                            }
                        }

                        resultado.ItensProcessados.Add(new ItemProcessado
                        {
                            Id = novoUsuario.Id.ToString(),
                            Identificador = novoUsuario.Email!,
                            Status = "Sucesso",
                            Mensagem = $"Usuário criado com sucesso{(string.IsNullOrEmpty(dadosUsuario.Senha) ? $" (senha gerada: {senha})" : "")}"
                        });
                        resultado.TotalSucessos++;
                    }
                    else
                    {
                        resultado.ItensProcessados.Add(new ItemProcessado
                        {
                            Id = "N/A",
                            Identificador = dadosUsuario.Email,
                            Status = "Erro",
                            Mensagem = $"Erro ao criar usuário: {string.Join(", ", resultadoCriacao.Errors.Select(e => e.Description))}"
                        });
                        resultado.TotalErros++;
                    }
                }
                catch (Exception ex)
                {
                    resultado.ItensProcessados.Add(new ItemProcessado
                    {
                        Id = "N/A",
                        Identificador = dadosUsuario.Email ?? "N/A",
                        Status = "Erro",
                        Mensagem = $"Erro interno: {ex.Message}"
                    });
                    resultado.TotalErros++;
                }
            }

            await transaction.CommitAsync();

            resultado.ConcluidoEm = DateTime.UtcNow;
            resultado.Sucesso = resultado.TotalErros == 0;
            resultado.Mensagem = $"Operação concluída: {resultado.TotalSucessos} sucessos, {resultado.TotalErros} erros";

            return resultado;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Atualiza usuários em lote
    /// </summary>
    private async Task<RespostaOperacaoLote> ExecutarLoteAtualizar(SolicitacaoOperacaoLote request, int usuarioLogadoId)
    {
        var resultado = new RespostaOperacaoLote
        {
            TipoOperacao = "atualizar",
            JobId = Guid.NewGuid().ToString(),
            IniciadoEm = DateTime.UtcNow
        };

        if (request.DadosUsuarios?.Any() != true)
        {
            resultado.Sucesso = false;
            resultado.Mensagem = "Nenhum dado de usuário fornecido para atualização";
            resultado.ConcluidoEm = DateTime.UtcNow;
            return resultado;
        }

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            resultado.TotalProcessados = request.DadosUsuarios.Count;

            foreach (var dadosUsuario in request.DadosUsuarios)
            {
                try
                {
                    if (!dadosUsuario.Id.HasValue)
                    {
                        resultado.ItensProcessados.Add(new ItemProcessado
                        {
                            Id = "N/A",
                            Identificador = dadosUsuario.Email ?? "N/A",
                            Status = "Erro",
                            Mensagem = "ID do usuário é obrigatório para atualização"
                        });
                        resultado.TotalErros++;
                        continue;
                    }

                    var usuario = await _userManager.FindByIdAsync(dadosUsuario.Id.Value.ToString());
                    if (usuario == null)
                    {
                        resultado.ItensProcessados.Add(new ItemProcessado
                        {
                            Id = dadosUsuario.Id.Value.ToString(),
                            Identificador = dadosUsuario.Email ?? "N/A",
                            Status = "Erro",
                            Mensagem = "Usuário não encontrado"
                        });
                        resultado.TotalErros++;
                        continue;
                    }

                    var houveMudancas = false;

                    // ✅ ATUALIZAR CAMPOS SE FORNECIDOS
                    if (!string.IsNullOrEmpty(dadosUsuario.Nome) && dadosUsuario.Nome != usuario.Nome)
                    {
                        usuario.Nome = dadosUsuario.Nome;
                        houveMudancas = true;
                    }

                    if (!string.IsNullOrEmpty(dadosUsuario.Sobrenome) && dadosUsuario.Sobrenome != usuario.Sobrenome)
                    {
                        usuario.Sobrenome = dadosUsuario.Sobrenome;
                        houveMudancas = true;
                    }

                    if (dadosUsuario.Telefone != usuario.PhoneNumber)
                    {
                        usuario.PhoneNumber = dadosUsuario.Telefone;
                        houveMudancas = true;
                    }

                    if (dadosUsuario.Ativo.HasValue && dadosUsuario.Ativo.Value != usuario.Ativo)
                    {
                        usuario.Ativo = dadosUsuario.Ativo.Value;
                        houveMudancas = true;
                    }

                    if (dadosUsuario.Observacoes != usuario.Observacoes)
                    {
                        usuario.Observacoes = dadosUsuario.Observacoes;
                        houveMudancas = true;
                    }

                    if (houveMudancas)
                    {
                        usuario.NomeCompleto = $"{usuario.Nome} {usuario.Sobrenome}";
                        usuario.DataAtualizacao = DateTime.UtcNow;

                        var resultadoUpdate = await _userManager.UpdateAsync(usuario);
                        if (resultadoUpdate.Succeeded)
                        {
                            resultado.ItensProcessados.Add(new ItemProcessado
                            {
                                Id = usuario.Id.ToString(),
                                Identificador = usuario.Email!,
                                Status = "Sucesso",
                                Mensagem = "Usuário atualizado com sucesso"
                            });
                            resultado.TotalSucessos++;
                        }
                        else
                        {
                            resultado.ItensProcessados.Add(new ItemProcessado
                            {
                                Id = usuario.Id.ToString(),
                                Identificador = usuario.Email!,
                                Status = "Erro",
                                Mensagem = $"Erro ao atualizar: {string.Join(", ", resultadoUpdate.Errors.Select(e => e.Description))}"
                            });
                            resultado.TotalErros++;
                        }
                    }
                    else
                    {
                        resultado.ItensProcessados.Add(new ItemProcessado
                        {
                            Id = usuario.Id.ToString(),
                            Identificador = usuario.Email!,
                            Status = "Ignorado",
                            Mensagem = "Nenhuma alteração necessária"
                        });
                        resultado.TotalIgnorados++;
                    }
                }
                catch (Exception ex)
                {
                    resultado.ItensProcessados.Add(new ItemProcessado
                    {
                        Id = dadosUsuario.Id?.ToString() ?? "N/A",
                        Identificador = dadosUsuario.Email ?? "N/A",
                        Status = "Erro",
                        Mensagem = $"Erro interno: {ex.Message}"
                    });
                    resultado.TotalErros++;
                }
            }

            await transaction.CommitAsync();

            resultado.ConcluidoEm = DateTime.UtcNow;
            resultado.Sucesso = resultado.TotalErros == 0;
            resultado.Mensagem = $"Operação concluída: {resultado.TotalSucessos} sucessos, {resultado.TotalErros} erros, {resultado.TotalIgnorados} ignorados";

            return resultado;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Gera senha aleatória segura
    /// </summary>
    private string GerarSenhaAleatoria()
    {
        const string caracteres = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        var random = new Random();
        return new string(Enumerable.Repeat(caracteres, 12)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    /// <summary>
    /// Gera exportação em formato CSV
    /// </summary>
    private string GerarExportacaoCSV(IEnumerable<object> dados)
    {
        var csv = new System.Text.StringBuilder();

        // ✅ CABEÇALHO
        csv.AppendLine("Id,Email,Nome,Sobrenome,NomeCompleto,Telefone,Ativo,EmailConfirmado,TelefoneConfirmado,DataCriacao,DataAtualizacao,UltimoLogin,Papeis,Observacoes");

        // ✅ DADOS
        foreach (dynamic item in dados)
        {
            var linha = $"{item.Id}," +
                       $"\"{item.Email}\"," +
                       $"\"{item.Nome}\"," +
                       $"\"{item.Sobrenome}\"," +
                       $"\"{item.NomeCompleto}\"," +
                       $"\"{item.Telefone ?? ""}\"," +
                       $"{item.Ativo}," +
                       $"{item.EmailConfirmado}," +
                       $"{item.TelefoneConfirmado}," +
                       $"{item.DataCriacao:yyyy-MM-dd HH:mm:ss}," +
                       $"{item.DataAtualizacao?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""}," +
                       $"{item.UltimoLogin?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""}," +
                       $"\"{string.Join(";", item.Papeis)}\"," +
                       $"\"{item.Observacoes ?? ""}\"";

            csv.AppendLine(linha);
        }

        return csv.ToString();
    }

    /// <summary>
    /// Executa atualização de usuários em lote
    /// </summary>
    private async Task ExecutarAtualizacaoLote(List<DadosUsuarioLote> dados, RespostaOperacaoLote resposta, int usuarioLogadoId)
    {
        foreach (var dadosUsuario in dados)
        {
            var item = new ItemProcessado
            {
                Id = dadosUsuario.Id?.ToString() ?? "novo",
                Identificador = dadosUsuario.Email ?? "N/A"
            };

            try
            {
                if (!dadosUsuario.Id.HasValue)
                {
                    item.Status = "Erro";
                    item.Mensagem = "ID do usuário é obrigatório para atualização";
                    resposta.TotalErros++;
                    continue;
                }

                var usuario = await _userManager.FindByIdAsync(dadosUsuario.Id.Value.ToString());
                if (usuario == null)
                {
                    item.Status = "Erro";
                    item.Mensagem = "Usuário não encontrado";
                    resposta.TotalErros++;
                    continue;
                }

                // ✅ ATUALIZAR CAMPOS FORNECIDOS
                bool alterado = false;

                if (!string.IsNullOrEmpty(dadosUsuario.Email) && dadosUsuario.Email != usuario.Email)
                {
                    // Verificar se email já existe
                    var emailExiste = await _userManager.FindByEmailAsync(dadosUsuario.Email);
                    if (emailExiste != null && emailExiste.Id != usuario.Id)
                    {
                        item.Status = "Erro";
                        item.Mensagem = "Email já existe para outro usuário";
                        resposta.TotalErros++;
                        continue;
                    }

                    usuario.Email = dadosUsuario.Email;
                    usuario.UserName = dadosUsuario.Email;
                    alterado = true;
                }

                if (!string.IsNullOrEmpty(dadosUsuario.Nome) && dadosUsuario.Nome != usuario.Nome)
                {
                    usuario.Nome = dadosUsuario.Nome;
                    alterado = true;
                }

                if (!string.IsNullOrEmpty(dadosUsuario.Sobrenome) && dadosUsuario.Sobrenome != usuario.Sobrenome)
                {
                    usuario.Sobrenome = dadosUsuario.Sobrenome;
                    alterado = true;
                }

                if (!string.IsNullOrEmpty(dadosUsuario.Telefone) && dadosUsuario.Telefone != usuario.PhoneNumber)
                {
                    usuario.PhoneNumber = dadosUsuario.Telefone;
                    alterado = true;
                }

                if (dadosUsuario.Ativo.HasValue && dadosUsuario.Ativo.Value != usuario.Ativo)
                {
                    // Validação: não pode desativar próprio usuário
                    if (!dadosUsuario.Ativo.Value && usuario.Id == usuarioLogadoId)
                    {
                        item.Status = "Ignorado";
                        item.Mensagem = "Não é possível desativar seu próprio usuário";
                        resposta.TotalIgnorados++;
                        continue;
                    }

                    usuario.Ativo = dadosUsuario.Ativo.Value;
                    alterado = true;
                }

                if (dadosUsuario.EmailConfirmado.HasValue && dadosUsuario.EmailConfirmado.Value != usuario.EmailConfirmed)
                {
                    usuario.EmailConfirmed = dadosUsuario.EmailConfirmado.Value;
                    alterado = true;
                }

                if (dadosUsuario.TelefoneConfirmado.HasValue && dadosUsuario.TelefoneConfirmado.Value != usuario.PhoneNumberConfirmed)
                {
                    usuario.PhoneNumberConfirmed = dadosUsuario.TelefoneConfirmado.Value;
                    alterado = true;
                }

                if (!string.IsNullOrEmpty(dadosUsuario.Observacoes) && dadosUsuario.Observacoes != usuario.Observacoes)
                {
                    usuario.Observacoes = dadosUsuario.Observacoes;
                    alterado = true;
                }

                if (alterado)
                {
                    usuario.DataAtualizacao = DateTime.UtcNow;
                    usuario.NomeCompleto = $"{usuario.Nome} {usuario.Sobrenome}";
                }

                // ✅ ALTERAR SENHA SE FORNECIDA
                if (!string.IsNullOrEmpty(dadosUsuario.Senha))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(usuario);
                    var resultadoSenha = await _userManager.ResetPasswordAsync(usuario, token, dadosUsuario.Senha);

                    if (!resultadoSenha.Succeeded)
                    {
                        item.Status = "Erro";
                        item.Mensagem = $"Erro ao alterar senha: {string.Join("; ", resultadoSenha.Errors.Select(e => e.Description))}";
                        resposta.TotalErros++;
                        continue;
                    }
                    alterado = true;
                }

                // ✅ SALVAR ALTERAÇÕES DO USUÁRIO
                if (alterado)
                {
                    var resultado = await _userManager.UpdateAsync(usuario);
                    if (!resultado.Succeeded)
                    {
                        item.Status = "Erro";
                        item.Mensagem = string.Join("; ", resultado.Errors.Select(e => e.Description));
                        resposta.TotalErros++;
                        continue;
                    }
                }

                // ✅ GERENCIAR PAPÉIS SE ESPECIFICADOS
                if (dadosUsuario.Papeis?.Any() == true)
                {
                    var papeisAtuais = await _userManager.GetRolesAsync(usuario);
                    var papeisRemover = papeisAtuais.Except(dadosUsuario.Papeis).ToList();
                    var papeisAdicionar = dadosUsuario.Papeis.Except(papeisAtuais).ToList();

                    if (papeisRemover.Any())
                    {
                        await _userManager.RemoveFromRolesAsync(usuario, papeisRemover);
                    }

                    if (papeisAdicionar.Any())
                    {
                        await _userManager.AddToRolesAsync(usuario, papeisAdicionar);
                    }

                    if (papeisRemover.Any() || papeisAdicionar.Any())
                    {
                        alterado = true;
                    }
                }

                if (alterado || dadosUsuario.Papeis?.Any() == true)
                {
                    item.Status = "Sucesso";
                    item.Mensagem = "Usuário atualizado com sucesso";
                    item.Identificador = usuario.Email!;
                    resposta.TotalSucessos++;
                }
                else
                {
                    item.Status = "Ignorado";
                    item.Mensagem = "Nenhuma alteração necessária";
                    item.Identificador = usuario.Email!;
                    resposta.TotalIgnorados++;
                }
            }
            catch (Exception ex)
            {
                item.Status = "Erro";
                item.Mensagem = ex.Message;
                resposta.TotalErros++;
            }

            resposta.ItensProcessados.Add(item);
            resposta.TotalProcessados++;
        }
    }

    /// <summary>
    /// Executa exclusão de usuários em lote
    /// </summary>
    private async Task ExecutarExclusaoLote(List<int> usuariosIds, RespostaOperacaoLote resposta, int usuarioLogadoId)
    {
        foreach (var usuarioId in usuariosIds)
        {
            var item = new ItemProcessado
            {
                Id = usuarioId.ToString(),
                Identificador = usuarioId.ToString()
            };

            try
            {
                var usuario = await _userManager.FindByIdAsync(usuarioId.ToString());
                if (usuario == null)
                {
                    item.Status = "Erro";
                    item.Mensagem = "Usuário não encontrado";
                    resposta.TotalErros++;
                    continue;
                }

                // ✅ VALIDAÇÕES DE SEGURANÇA
                if (usuario.Id == usuarioLogadoId)
                {
                    item.Status = "Ignorado";
                    item.Mensagem = "Não é possível excluir seu próprio usuário";
                    resposta.TotalIgnorados++;
                    continue;
                }

                // Verificar se é SuperAdmin
                var papeis = await _userManager.GetRolesAsync(usuario);
                if (papeis.Contains("SuperAdmin"))
                {
                    var totalSuperAdmins = await _context.Users
                        .Where(u => u.UsuarioPapeis.Any(up => up.Papel.Name == "SuperAdmin" && up.Ativo) && u.Ativo)
                        .CountAsync();

                    if (totalSuperAdmins <= 1)
                    {
                        item.Status = "Ignorado";
                        item.Mensagem = "Não é possível excluir o último Super Administrador";
                        resposta.TotalIgnorados++;
                        continue;
                    }
                }

                // ✅ USAR SOFT DELETE (desativação)
                usuario.Ativo = false;
                usuario.DataAtualizacao = DateTime.UtcNow;

                // Desativar relacionamentos
                foreach (var usuarioPapel in usuario.UsuarioPapeis.Where(up => up.Ativo))
                {
                    usuarioPapel.Ativo = false;
                }

                foreach (var usuarioGrupo in usuario.UsuarioGrupos.Where(ug => ug.Ativo))
                {
                    usuarioGrupo.Ativo = false;
                }

                var resultado = await _userManager.UpdateAsync(usuario);
                await _context.SaveChangesAsync(); // Para salvar relacionamentos

                if (resultado.Succeeded)
                {
                    item.Status = "Sucesso";
                    item.Mensagem = "Usuário excluído (desativado) com sucesso";
                    item.Identificador = usuario.Email!;
                    resposta.TotalSucessos++;
                }
                else
                {
                    item.Status = "Erro";
                    item.Mensagem = string.Join("; ", resultado.Errors.Select(e => e.Description));
                    resposta.TotalErros++;
                }
            }
            catch (Exception ex)
            {
                item.Status = "Erro";
                item.Mensagem = ex.Message;
                resposta.TotalErros++;
            }

            resposta.ItensProcessados.Add(item);
            resposta.TotalProcessados++;
        }
    }

    /// <summary>
    /// Executa gerenciamento de papéis em lote
    /// </summary>
    private async Task ExecutarGerenciamentoPapeisLote(List<int> usuariosIds, string papeisString,
        string operacao, RespostaOperacaoLote resposta, int usuarioLogadoId)
    {
        var papeis = string.IsNullOrEmpty(papeisString) ?
            new List<string>() :
            papeisString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                       .Select(p => p.Trim())
                       .Where(p => !string.IsNullOrEmpty(p))
                       .ToList();

        foreach (var usuarioId in usuariosIds)
        {
            var item = new ItemProcessado
            {
                Id = usuarioId.ToString(),
                Identificador = usuarioId.ToString()
            };

            try
            {
                var usuario = await _userManager.FindByIdAsync(usuarioId.ToString());
                if (usuario == null)
                {
                    item.Status = "Erro";
                    item.Mensagem = "Usuário não encontrado";
                    resposta.TotalErros++;
                    continue;
                }

                if (!usuario.Ativo)
                {
                    item.Status = "Ignorado";
                    item.Mensagem = "Usuário está inativo";
                    resposta.TotalIgnorados++;
                    continue;
                }

                // ✅ VALIDAÇÕES DE SEGURANÇA PARA SUPERADMIN
                if (usuario.Id == usuarioLogadoId && (operacao == "remover" || operacao == "limpar"))
                {
                    var papeisAtuais = await _userManager.GetRolesAsync(usuario);
                    if (papeisAtuais.Contains("SuperAdmin"))
                    {
                        var tentandoRemoverSuperAdmin = operacao == "limpar" ||
                            (operacao == "remover" && papeis.Contains("SuperAdmin"));

                        if (tentandoRemoverSuperAdmin)
                        {
                            var totalSuperAdmins = await _context.Users
                                .Where(u => u.UsuarioPapeis.Any(up => up.Papel.Name == "SuperAdmin" && up.Ativo) && u.Ativo)
                                .CountAsync();

                            if (totalSuperAdmins <= 1)
                            {
                                item.Status = "Ignorado";
                                item.Mensagem = "Não é possível remover papel SuperAdmin do último administrador";
                                resposta.TotalIgnorados++;
                                continue;
                            }
                        }
                    }
                }

                // ✅ EXECUTAR OPERAÇÃO DE PAPÉIS
                bool sucesso = false;
                string mensagem = "";

                switch (operacao.ToLower())
                {
                    case "adicionar":
                        sucesso = await AdicionarPapeisUsuario(usuario, papeis);
                        mensagem = sucesso ? $"Papéis adicionados: {string.Join(", ", papeis)}" : "Erro ao adicionar papéis";
                        break;

                    case "remover":
                        sucesso = await RemoverPapeisUsuario(usuario, papeis);
                        mensagem = sucesso ? $"Papéis removidos: {string.Join(", ", papeis)}" : "Erro ao remover papéis";
                        break;

                    case "limpar":
                        sucesso = await LimparTodosPapeisUsuario(usuario);
                        mensagem = sucesso ? "Todos os papéis removidos" : "Erro ao limpar papéis";
                        break;

                    default:
                        item.Status = "Erro";
                        item.Mensagem = $"Operação inválida: {operacao}";
                        resposta.TotalErros++;
                        continue;
                }

                // ✅ ATUALIZAR METADADOS DOS RELACIONAMENTOS
                if (sucesso && (operacao == "adicionar"))
                {
                    await AtualizarMetadadosPapeis(usuario.Id, papeis, usuarioLogadoId);
                }

                if (sucesso)
                {
                    item.Status = "Sucesso";
                    item.Mensagem = mensagem;
                    item.Identificador = usuario.Email!;
                    resposta.TotalSucessos++;
                }
                else
                {
                    item.Status = "Erro";
                    item.Mensagem = mensagem;
                    resposta.TotalErros++;
                }
            }
            catch (Exception ex)
            {
                item.Status = "Erro";
                item.Mensagem = ex.Message;
                resposta.TotalErros++;
            }

            resposta.ItensProcessados.Add(item);
            resposta.TotalProcessados++;
        }
    }

    /// <summary>
    /// Executa reset de senha em lote
    /// </summary>
    private async Task ExecutarResetSenhaLote(List<int> usuariosIds, RespostaOperacaoLote resposta, int usuarioLogadoId)
    {
        foreach (var usuarioId in usuariosIds)
        {
            var item = new ItemProcessado
            {
                Id = usuarioId.ToString(),
                Identificador = usuarioId.ToString()
            };

            try
            {
                var usuario = await _userManager.FindByIdAsync(usuarioId.ToString());
                if (usuario == null)
                {
                    item.Status = "Erro";
                    item.Mensagem = "Usuário não encontrado";
                    resposta.TotalErros++;
                    continue;
                }

                if (!usuario.Ativo)
                {
                    item.Status = "Ignorado";
                    item.Mensagem = "Usuário está inativo";
                    resposta.TotalIgnorados++;
                    continue;
                }

                // ✅ GERAR SENHA TEMPORÁRIA SEGURA
                var senhaTemporaria = GerarSenhaTemporaria();

                // Resetar senha
                var token = await _userManager.GeneratePasswordResetTokenAsync(usuario);
                var resultado = await _userManager.ResetPasswordAsync(usuario, token, senhaTemporaria);

                if (resultado.Succeeded)
                {
                    // ✅ FORÇAR ALTERAÇÃO DE SENHA NO PRÓXIMO LOGIN
                    usuario.DataAtualizacao = DateTime.UtcNow;
                    // Nota: Implementar campo "DeveAlterarSenha" no modelo Usuario futuramente

                    await _userManager.UpdateAsync(usuario);

                    item.Status = "Sucesso";
                    item.Mensagem = $"Senha resetada. Nova senha temporária: {senhaTemporaria}";
                    item.Identificador = usuario.Email!;
                    resposta.TotalSucessos++;
                }
                else
                {
                    item.Status = "Erro";
                    item.Mensagem = string.Join("; ", resultado.Errors.Select(e => e.Description));
                    resposta.TotalErros++;
                }
            }
            catch (Exception ex)
            {
                item.Status = "Erro";
                item.Mensagem = ex.Message;
                resposta.TotalErros++;
            }

            resposta.ItensProcessados.Add(item);
            resposta.TotalProcessados++;
        }
    }

    /// <summary>
    /// Executa confirmação de email ou telefone em lote
    /// </summary>
    private async Task ExecutarConfirmacaoLote(List<int> usuariosIds, string tipo,
        RespostaOperacaoLote resposta, int usuarioLogadoId)
    {
        foreach (var usuarioId in usuariosIds)
        {
            var item = new ItemProcessado
            {
                Id = usuarioId.ToString(),
                Identificador = usuarioId.ToString()
            };

            try
            {
                var usuario = await _userManager.FindByIdAsync(usuarioId.ToString());
                if (usuario == null)
                {
                    item.Status = "Erro";
                    item.Mensagem = "Usuário não encontrado";
                    resposta.TotalErros++;
                    continue;
                }

                bool sucesso = false;
                string mensagem = "";

                switch (tipo.ToLower())
                {
                    case "email":
                        if (usuario.EmailConfirmed)
                        {
                            item.Status = "Ignorado";
                            item.Mensagem = "Email já está confirmado";
                            resposta.TotalIgnorados++;
                            continue;
                        }

                        if (string.IsNullOrEmpty(usuario.Email))
                        {
                            item.Status = "Erro";
                            item.Mensagem = "Usuário não possui email";
                            resposta.TotalErros++;
                            continue;
                        }

                        // ✅ CONFIRMAR EMAIL ADMINISTRATIVAMENTE
                        var tokenEmail = await _userManager.GenerateEmailConfirmationTokenAsync(usuario);
                        var resultadoEmail = await _userManager.ConfirmEmailAsync(usuario, tokenEmail);

                        sucesso = resultadoEmail.Succeeded;
                        mensagem = sucesso ? "Email confirmado com sucesso" :
                            string.Join("; ", resultadoEmail.Errors.Select(e => e.Description));
                        break;

                    case "telefone":
                        if (usuario.PhoneNumberConfirmed)
                        {
                            item.Status = "Ignorado";
                            item.Mensagem = "Telefone já está confirmado";
                            resposta.TotalIgnorados++;
                            continue;
                        }

                        if (string.IsNullOrEmpty(usuario.PhoneNumber))
                        {
                            item.Status = "Erro";
                            item.Mensagem = "Usuário não possui telefone";
                            resposta.TotalErros++;
                            continue;
                        }

                        // ✅ CONFIRMAR TELEFONE ADMINISTRATIVAMENTE
                        var tokenTelefone = await _userManager.GenerateChangePhoneNumberTokenAsync(usuario, usuario.PhoneNumber);
                        var resultadoTelefone = await _userManager.ChangePhoneNumberAsync(usuario, usuario.PhoneNumber, tokenTelefone);

                        sucesso = resultadoTelefone.Succeeded;
                        mensagem = sucesso ? "Telefone confirmado com sucesso" :
                            string.Join("; ", resultadoTelefone.Errors.Select(e => e.Description));
                        break;

                    default:
                        item.Status = "Erro";
                        item.Mensagem = $"Tipo de confirmação inválido: {tipo}";
                        resposta.TotalErros++;
                        continue;
                }

                if (sucesso)
                {
                    usuario.DataAtualizacao = DateTime.UtcNow;
                    await _userManager.UpdateAsync(usuario);

                    item.Status = "Sucesso";
                    item.Mensagem = mensagem;
                    item.Identificador = usuario.Email!;
                    resposta.TotalSucessos++;
                }
                else
                {
                    item.Status = "Erro";
                    item.Mensagem = mensagem;
                    resposta.TotalErros++;
                }
            }
            catch (Exception ex)
            {
                item.Status = "Erro";
                item.Mensagem = ex.Message;
                resposta.TotalErros++;
            }

            resposta.ItensProcessados.Add(item);
            resposta.TotalProcessados++;
        }
    }

    /// <summary>
    /// Adiciona papéis a um usuário
    /// </summary>
    private async Task<bool> AdicionarPapeisUsuario(Usuario usuario, List<string> papeis)
    {
        try
        {
            var papeisAtuais = await _userManager.GetRolesAsync(usuario);
            var papeisNovos = papeis.Except(papeisAtuais).ToList();

            if (papeisNovos.Any())
            {
                var resultado = await _userManager.AddToRolesAsync(usuario, papeisNovos);
                return resultado.Succeeded;
            }

            return true; // Nenhum papel novo para adicionar
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Remove papéis de um usuário
    /// </summary>
    private async Task<bool> RemoverPapeisUsuario(Usuario usuario, List<string> papeis)
    {
        try
        {
            var papeisAtuais = await _userManager.GetRolesAsync(usuario);
            var papeisRemover = papeis.Intersect(papeisAtuais).ToList();

            if (papeisRemover.Any())
            {
                var resultado = await _userManager.RemoveFromRolesAsync(usuario, papeisRemover);
                return resultado.Succeeded;
            }

            return true; // Nenhum papel para remover
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Remove todos os papéis de um usuário
    /// </summary>
    private async Task<bool> LimparTodosPapeisUsuario(Usuario usuario)
    {
        try
        {
            var papeisAtuais = await _userManager.GetRolesAsync(usuario);

            if (papeisAtuais.Any())
            {
                var resultado = await _userManager.RemoveFromRolesAsync(usuario, papeisAtuais);
                return resultado.Succeeded;
            }

            return true; // Nenhum papel para remover
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gera senha temporária segura
    /// </summary>
    private string GerarSenhaTemporaria()
    {
        const string chars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
        const string especiais = "!@#$%&*";
        
        var random = new Random();
        var senha = new char[8];
        
        // Garantir pelo menos uma maiúscula, uma minúscula, um número e um especial
        senha[0] = "ABCDEFGHJKLMNOPQRSTUVWXYZ"[random.Next(23)];
        senha[1] = "abcdefghijkmnopqrstuvwxyz"[random.Next(23)];
        senha[2] = "23456789"[random.Next(8)];
        senha[3] = especiais[random.Next(especiais.Length)];
        
        // Preencher o resto aleatoriamente
        for (int i = 4; i < senha.Length; i++)
        {
            senha[i] = chars[random.Next(chars.Length)];
        }
        
        // Embaralhar
        for (int i = senha.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (senha[i], senha[j]) = (senha[j], senha[i]);
        }
        
        return new string(senha);
    }

    #endregion

    #region Métodos Auxiliares de Aplicações

    private bool EhProprioUsuario(int usuarioId)
    {
        var usuarioLogadoId = ObterUsuarioLogadoId();
        return usuarioLogadoId == usuarioId;
    }

    private IQueryable<UsuarioAplicacao> AplicarFiltrosAplicacaoUsuario(IQueryable<UsuarioAplicacao> query, FiltrosAplicacaoUsuario filtros)
    {
        if (!string.IsNullOrEmpty(filtros.Nome))
        {
            query = query.Where(ua => ua.Aplicacao.Nome.ToLower().Contains(filtros.Nome.ToLower()));
        }

        if (!string.IsNullOrEmpty(filtros.TipoAplicacao))
        {
            query = query.Where(ua => ua.Aplicacao.TipoAplicacao.Codigo == filtros.TipoAplicacao);
        }

        if (!string.IsNullOrEmpty(filtros.StatusAplicacao))
        {
            query = query.Where(ua => ua.Aplicacao.StatusAplicacao.Codigo == filtros.StatusAplicacao);
        }

        if (filtros.ApenasAtivas.HasValue && filtros.ApenasAtivas.Value)
        {
            query = query.Where(ua => ua.Ativo);
        }

        if (filtros.ApenasAprovados.HasValue && filtros.ApenasAprovados.Value)
        {
            query = query.Where(ua => ua.Aprovado);
        }

        if (filtros.NivelSegurancaMinimo.HasValue)
        {
            query = query.Where(ua => ua.Aplicacao.NivelSeguranca >= filtros.NivelSegurancaMinimo.Value);
        }

        if (filtros.NivelSegurancaMaximo.HasValue)
        {
            query = query.Where(ua => ua.Aplicacao.NivelSeguranca <= filtros.NivelSegurancaMaximo.Value);
        }

        if (filtros.DataSolicitacaoInicio.HasValue)
        {
            query = query.Where(ua => ua.DataSolicitacao >= filtros.DataSolicitacaoInicio.Value);
        }

        if (filtros.DataSolicitacaoFim.HasValue)
        {
            query = query.Where(ua => ua.DataSolicitacao <= filtros.DataSolicitacaoFim.Value);
        }

        if (!filtros.IncluirExpiradas)
        {
            query = query.Where(ua => !ua.DataExpiracao.HasValue || ua.DataExpiracao > DateTime.UtcNow);
        }

        return query;
    }

    private IQueryable<UsuarioAplicacao> AplicarOrdenacaoAplicacaoUsuario(IQueryable<UsuarioAplicacao> query, string? ordenarPor, string? direcao)
    {
        var desc = direcao?.ToLower() == "desc";

        return ordenarPor?.ToLower() switch
        {
            "nome" => desc ? query.OrderByDescending(ua => ua.Aplicacao.Nome) : query.OrderBy(ua => ua.Aplicacao.Nome),
            "datasolicitacao" => desc ? query.OrderByDescending(ua => ua.DataSolicitacao) : query.OrderBy(ua => ua.DataSolicitacao),
            "dataaprovacao" => desc ? query.OrderByDescending(ua => ua.DataAprovacao) : query.OrderBy(ua => ua.DataAprovacao),
            "nivelseguranca" => desc ? query.OrderByDescending(ua => ua.Aplicacao.NivelSeguranca) : query.OrderBy(ua => ua.Aplicacao.NivelSeguranca),
            _ => query.OrderBy(ua => ua.Aplicacao.Nome)
        };
    }

    private AplicacaoUsuario ConstruirAplicacaoUsuario(UsuarioAplicacao usuarioAplicacao)
    {
        return new AplicacaoUsuario
        {
            Id = usuarioAplicacao.Aplicacao.Id,
            Nome = usuarioAplicacao.Aplicacao.Nome,
            Codigo = usuarioAplicacao.Aplicacao.Codigo,
            Descricao = usuarioAplicacao.Aplicacao.Descricao,
            UrlBase = usuarioAplicacao.Aplicacao.UrlBase,
            TipoAplicacao = new TipoAplicacaoUsuario
            {
                Codigo = usuarioAplicacao.Aplicacao.TipoAplicacao.Codigo,
                Nome = usuarioAplicacao.Aplicacao.TipoAplicacao.Nome,
                Icone = usuarioAplicacao.Aplicacao.TipoAplicacao.Icone,
                Cor = usuarioAplicacao.Aplicacao.TipoAplicacao.Cor
            },
            StatusAplicacao = new StatusAplicacaoUsuario
            {
                Codigo = usuarioAplicacao.Aplicacao.StatusAplicacao.Codigo,
                Nome = usuarioAplicacao.Aplicacao.StatusAplicacao.Nome,
                CorFundo = usuarioAplicacao.Aplicacao.StatusAplicacao.CorFundo,
                CorTexto = usuarioAplicacao.Aplicacao.StatusAplicacao.CorTexto,
                Icone = usuarioAplicacao.Aplicacao.StatusAplicacao.Icone,
                PermiteAcesso = usuarioAplicacao.Aplicacao.StatusAplicacao.PermiteAcesso,
                MensagemUsuario = usuarioAplicacao.Aplicacao.StatusAplicacao.MensagemUsuario
            },
            Versao = usuarioAplicacao.Aplicacao.Versao,
            NivelSeguranca = usuarioAplicacao.Aplicacao.NivelSeguranca,
            StatusAcesso = ObterStatusAcessoUsuario(usuarioAplicacao),
            DataSolicitacao = usuarioAplicacao.DataSolicitacao,
            DataAprovacao = usuarioAplicacao.DataAprovacao,
            DataExpiracao = usuarioAplicacao.DataExpiracao,
            Justificativa = usuarioAplicacao.Justificativa,
            ObservacoesAprovacao = usuarioAplicacao.ObservacoesAprovacao,
            AprovadoPor = usuarioAplicacao.AprovadoPor?.Nome + " " + usuarioAplicacao.AprovadoPor?.Sobrenome,
            Ativo = usuarioAplicacao.Ativo,
            Aprovado = usuarioAplicacao.Aprovado,
            ConfiguracoesUsuario = usuarioAplicacao.ConfiguracoesUsuario,
            TotalPermissoes = ObterTotalPermissoesUsuarioAplicacao(usuarioAplicacao.UsuarioId, usuarioAplicacao.AplicacaoId)
        };
    }

    private StatusAcessoUsuario ObterStatusAcessoUsuario(UsuarioAplicacao usuarioAplicacao)
    {
        if (!usuarioAplicacao.Ativo)
        {
            return new StatusAcessoUsuario
            {
                Codigo = "suspenso",
                Nome = "Suspenso",
                CorFundo = "#6c757d",
                CorTexto = "#ffffff",
                Icone = "⏸️"
            };
        }

        if (!usuarioAplicacao.Aprovado)
        {
            return new StatusAcessoUsuario
            {
                Codigo = "pendente",
                Nome = "Pendente",
                CorFundo = "#ffc107",
                CorTexto = "#000000",
                Icone = "⏳"
            };
        }

        if (usuarioAplicacao.DataExpiracao.HasValue && usuarioAplicacao.DataExpiracao <= DateTime.UtcNow)
        {
            return new StatusAcessoUsuario
            {
                Codigo = "expirado",
                Nome = "Expirado",
                CorFundo = "#dc3545",
                CorTexto = "#ffffff",
                Icone = "⏰"
            };
        }

        return new StatusAcessoUsuario
        {
            Codigo = "aprovado",
            Nome = "Aprovado",
            CorFundo = "#28a745",
            CorTexto = "#ffffff",
            Icone = "✅"
        };
    }

    private int ObterTotalPermissoesUsuarioAplicacao(int usuarioId, int aplicacaoId)
    {
        // Implementar lógica para contar permissões específicas do usuário na aplicação
        // baseado nos papéis que ele possui
        return 0; // Placeholder
    }

    private AplicacaoDisponivelUsuario ConstruirAplicacaoDisponivel(Aplicacao aplicacao, Usuario usuario)
    {
        return new AplicacaoDisponivelUsuario
        {
            Id = aplicacao.Id,
            Nome = aplicacao.Nome,
            Codigo = aplicacao.Codigo,
            Descricao = aplicacao.Descricao,
            UrlBase = aplicacao.UrlBase,
            TipoAplicacao = new TipoAplicacaoUsuario
            {
                Codigo = aplicacao.TipoAplicacao.Codigo,
                Nome = aplicacao.TipoAplicacao.Nome,
                Icone = aplicacao.TipoAplicacao.Icone,
                Cor = aplicacao.TipoAplicacao.Cor
            },
            StatusAplicacao = new StatusAplicacaoUsuario
            {
                Codigo = aplicacao.StatusAplicacao.Codigo,
                Nome = aplicacao.StatusAplicacao.Nome,
                CorFundo = aplicacao.StatusAplicacao.CorFundo,
                CorTexto = aplicacao.StatusAplicacao.CorTexto,
                Icone = aplicacao.StatusAplicacao.Icone,
                PermiteAcesso = aplicacao.StatusAplicacao.PermiteAcesso,
                MensagemUsuario = aplicacao.StatusAplicacao.MensagemUsuario
            },
            NivelSeguranca = aplicacao.NivelSeguranca,
            RequerAprovacao = aplicacao.RequerAprovacao,
            PermiteAutoRegistro = aplicacao.PermiteAutoRegistro,
            StatusUsuario = new StatusUsuarioAplicacao
            {
                Codigo = "sem_acesso",
                Nome = "Sem Acesso",
                CorFundo = "#f8f9fa",
                CorTexto = "#6c757d",
                Icone = "❌",
                PodeSolicitar = aplicacao.StatusAplicacao.PermiteNovoUsuario,
                PodeAcessar = false,
                Motivo = aplicacao.StatusAplicacao.PermiteNovoUsuario ? null : "Aplicação não aceita novos usuários"
            },
            TempoAprovacaoEstimado = aplicacao.RequerAprovacao ? "2-5 dias úteis" : "Imediato",
            ResponsaveisAprovacao = ObterResponsaveisAprovacao(aplicacao.Id)
        };
    }

    private List<string> ObterResponsaveisAprovacao(int aplicacaoId)
    {
        // Implementar lógica para obter lista de responsáveis pela aprovação
        // baseado em papéis ou configurações específicas
        return new List<string> { "Administradores do Sistema" }; // Placeholder
    }

    private async Task<List<AplicacaoUsuario>> ObterAplicacoesUsuarioAsync(int usuarioId)
    {
        var usuariosAplicacao = await _context.UsuariosAplicacao
            .Include(ua => ua.Aplicacao)
                .ThenInclude(a => a.TipoAplicacao)
            .Include(ua => ua.Aplicacao)
                .ThenInclude(a => a.StatusAplicacao)
            .Include(ua => ua.AprovadoPor)
            .Where(ua => ua.UsuarioId == usuarioId)
            .ToListAsync();

        return usuariosAplicacao.Select(ua => ConstruirAplicacaoUsuario(ua)).ToList();
    }

    private async Task<AplicacaoUsuario?> ObterAplicacaoUsuarioAsync(int usuarioId, int aplicacaoId)
    {
        var usuarioAplicacao = await _context.UsuariosAplicacao
            .Include(ua => ua.Aplicacao)
                .ThenInclude(a => a.TipoAplicacao)
            .Include(ua => ua.Aplicacao)
                .ThenInclude(a => a.StatusAplicacao)
            .Include(ua => ua.AprovadoPor)
            .FirstOrDefaultAsync(ua => ua.UsuarioId == usuarioId && ua.AplicacaoId == aplicacaoId);

        return usuarioAplicacao != null ? ConstruirAplicacaoUsuario(usuarioAplicacao) : null;
    }

    // Métodos auxiliares para operações complexas que serão implementados...
    private async Task<ResultadoOperacao> ExecutarOperacaoAplicacoes(Usuario usuario, GerenciarAplicacoesRequest request, int usuarioLogadoId)
    {
        var inicio = DateTime.UtcNow;
        var alertas = new List<string>();
        var erros = new List<string>();

        try
        {
            if (string.IsNullOrWhiteSpace(request.Operacao))
            {
                return new ResultadoOperacao { Sucesso = false, Mensagem = "Operação não informada" };
            }

            // Estrategia de execução resiliente + transação (padrão do projeto)
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _context.Database.BeginTransactionAsync();
                try
                {
                    var op = request.Operacao.Trim().ToLower();

                    // Estado atual das aplicações do usuário
                    var atuais = await _context.UsuariosAplicacao
                        .Include(ua => ua.Aplicacao)
                            .ThenInclude(a => a.StatusAplicacao)
                        .Where(ua => ua.UsuarioId == usuario.Id)
                        .ToListAsync();

                    // Pré-carregar aplicações solicitadas quando fizer sentido
                    List<Aplicacao> appsSelecionadas = new();
                    if (request.AplicacoesIds?.Any() == true)
                    {
                        appsSelecionadas = await _context.Aplicacoes
                            .Include(a => a.StatusAplicacao)
                            .Where(a => request.AplicacoesIds.Contains(a.Id))
                            .ToListAsync();

                        // Validar IDs inexistentes
                        var idsEncontrados = appsSelecionadas.Select(a => a.Id).ToHashSet();
                        var idsInvalidos = request.AplicacoesIds.Where(id => !idsEncontrados.Contains(id)).ToList();
                        if (idsInvalidos.Any())
                            alertas.Add($"Aplicações não encontradas: {string.Join(", ", idsInvalidos)}");
                    }

                    int adicionadas = 0, removidas = 0, aprovadas = 0, rejeitadas = 0;

                    switch (op)
                    {
                        case "adicionar":
                        {
                            if (!appsSelecionadas.Any())
                            {
                                erros.Add("Nenhuma aplicação informada para adicionar");
                                break;
                            }

                            foreach (var app in appsSelecionadas)
                            {
                                var existente = atuais.FirstOrDefault(x => x.AplicacaoId == app.Id);
                                if (existente != null)
                                {
                                    alertas.Add($"Usuário já possui vínculo com a aplicação '{app.Nome}'");
                                    continue;
                                }

                                var aprovarAgora = request.AprovarAutomaticamente || (!app.RequerAprovacao && app.StatusAplicacao.PermiteAcesso);
                                var novo = new UsuarioAplicacao
                                {
                                    UsuarioId = usuario.Id,
                                    AplicacaoId = app.Id,
                                    Justificativa = request.Justificativa,
                                    ConfiguracoesUsuario = request.ConfiguracoesUsuario,
                                    Aprovado = aprovarAgora,
                                    DataAprovacao = aprovarAgora ? DateTime.UtcNow : null,
                                    AprovadoPorId = aprovarAgora ? usuarioLogadoId : null,
                                    DataExpiracao = request.DataExpiracao,
                                    Ativo = true
                                };

                                _context.UsuariosAplicacao.Add(novo);
                                adicionadas++;
                            }

                            await _context.SaveChangesAsync();
                            break;
                        }

                        case "remover":
                        {
                            if (!appsSelecionadas.Any())
                            {
                                erros.Add("Nenhuma aplicação informada para remover");
                                break;
                            }

                            foreach (var app in appsSelecionadas)
                            {
                                var existente = await _context.UsuariosAplicacao
                                    .FirstOrDefaultAsync(x => x.UsuarioId == usuario.Id && x.AplicacaoId == app.Id);
                                if (existente == null)
                                {
                                    alertas.Add($"Usuário não possui a aplicação '{app.Nome}'");
                                    continue;
                                }

                                _context.UsuariosAplicacao.Remove(existente);
                                removidas++;
                            }

                            await _context.SaveChangesAsync();
                            break;
                        }

                        case "substituir":
                        {
                            // Remove tudo que não está na lista e adiciona os que faltam
                            var idsAlvo = request.AplicacoesIds?.ToHashSet() ?? new HashSet<int>();

                            // Remover vínculos que não estão na nova lista
                            var paraRemover = atuais.Where(ua => !idsAlvo.Contains(ua.AplicacaoId)).ToList();
                            if (paraRemover.Any())
                            {
                                _context.UsuariosAplicacao.RemoveRange(paraRemover);
                                removidas += paraRemover.Count;
                            }

                            // Adicionar vínculos que faltam
                            var idsAtuais = atuais.Select(a => a.AplicacaoId).ToHashSet();
                            var idsParaAdicionar = idsAlvo.Except(idsAtuais).ToList();
                            if (idsParaAdicionar.Any())
                            {
                                var appsParaAdicionar = await _context.Aplicacoes
                                    .Include(a => a.StatusAplicacao)
                                    .Where(a => idsParaAdicionar.Contains(a.Id))
                                    .ToListAsync();

                                foreach (var app in appsParaAdicionar)
                                {
                                    var aprovarAgora = request.AprovarAutomaticamente || (!app.RequerAprovacao && app.StatusAplicacao.PermiteAcesso);
                                    _context.UsuariosAplicacao.Add(new UsuarioAplicacao
                                    {
                                        UsuarioId = usuario.Id,
                                        AplicacaoId = app.Id,
                                        Justificativa = request.Justificativa,
                                        ConfiguracoesUsuario = request.ConfiguracoesUsuario,
                                        Aprovado = aprovarAgora,
                                        DataAprovacao = aprovarAgora ? DateTime.UtcNow : null,
                                        AprovadoPorId = aprovarAgora ? usuarioLogadoId : null,
                                        DataExpiracao = request.DataExpiracao,
                                        Ativo = true
                                    });
                                    adicionadas++;
                                }
                            }

                            await _context.SaveChangesAsync();
                            break;
                        }

                        case "aprovar":
                        {
                            if (!appsSelecionadas.Any())
                            {
                                erros.Add("Nenhuma aplicação informada para aprovar");
                                break;
                            }

                            foreach (var app in appsSelecionadas)
                            {
                                var vinculo = await _context.UsuariosAplicacao
                                    .Include(ua => ua.Aplicacao)
                                    .FirstOrDefaultAsync(x => x.UsuarioId == usuario.Id && x.AplicacaoId == app.Id);

                                if (vinculo == null)
                                {
                                    alertas.Add($"Solicitação inexistente para a aplicação '{app.Nome}'");
                                    continue;
                                }

                                vinculo.Aprovado = true;
                                vinculo.Ativo = true;
                                vinculo.DataAprovacao = DateTime.UtcNow;
                                vinculo.AprovadoPorId = usuarioLogadoId;
                                if (!string.IsNullOrWhiteSpace(request.Observacoes))
                                    vinculo.ObservacoesAprovacao = request.Observacoes;
                                if (request.DataExpiracao.HasValue)
                                    vinculo.DataExpiracao = request.DataExpiracao;
                                if (!string.IsNullOrWhiteSpace(request.ConfiguracoesUsuario))
                                    vinculo.ConfiguracoesUsuario = request.ConfiguracoesUsuario;

                                aprovadas++;
                            }

                            await _context.SaveChangesAsync();
                            break;
                        }

                        case "rejeitar":
                        {
                            if (!appsSelecionadas.Any())
                            {
                                erros.Add("Nenhuma aplicação informada para rejeitar");
                                break;
                            }

                            foreach (var app in appsSelecionadas)
                            {
                                var vinculo = await _context.UsuariosAplicacao
                                    .Include(ua => ua.Aplicacao)
                                    .FirstOrDefaultAsync(x => x.UsuarioId == usuario.Id && x.AplicacaoId == app.Id);

                                if (vinculo == null)
                                {
                                    alertas.Add($"Solicitação inexistente para a aplicação '{app.Nome}'");
                                    continue;
                                }

                                vinculo.Aprovado = false;
                                vinculo.Ativo = false;
                                vinculo.DataAprovacao = DateTime.UtcNow;
                                vinculo.AprovadoPorId = usuarioLogadoId;
                                vinculo.ObservacoesAprovacao = !string.IsNullOrWhiteSpace(request.Observacoes)
                                    ? request.Observacoes
                                    : "Acesso rejeitado";
                                vinculo.DataExpiracao = null;

                                rejeitadas++;
                            }

                            await _context.SaveChangesAsync();
                            break;
                        }

                        case "suspender":
                        {
                            if (!appsSelecionadas.Any())
                            {
                                erros.Add("Nenhuma aplicação informada para suspender");
                                break;
                            }

                            foreach (var app in appsSelecionadas)
                            {
                                var vinculo = await _context.UsuariosAplicacao
                                    .FirstOrDefaultAsync(x => x.UsuarioId == usuario.Id && x.AplicacaoId == app.Id);

                                if (vinculo == null)
                                {
                                    alertas.Add($"Vínculo inexistente para a aplicação '{app.Nome}'");
                                    continue;
                                }

                                vinculo.Ativo = false;
                            }

                            await _context.SaveChangesAsync();
                            break;
                        }

                        case "reativar":
                        {
                            if (!appsSelecionadas.Any())
                            {
                                erros.Add("Nenhuma aplicação informada para reativar");
                                break;
                            }

                            foreach (var app in appsSelecionadas)
                            {
                                var vinculo = await _context.UsuariosAplicacao
                                    .FirstOrDefaultAsync(x => x.UsuarioId == usuario.Id && x.AplicacaoId == app.Id);

                                if (vinculo == null)
                                {
                                    alertas.Add($"Vínculo inexistente para a aplicação '{app.Nome}'");
                                    continue;
                                }

                                vinculo.Ativo = true;
                            }

                            await _context.SaveChangesAsync();
                            break;
                        }

                        default:
                            throw new NotSupportedException($"Operação não suportada: {request.Operacao}");
                    }

                    await tx.CommitAsync();
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }
            });

            var duracao = DateTime.UtcNow - inicio;
            var mensagem = request.Operacao.Trim().ToLower() switch
            {
                "adicionar" => "Aplicações adicionadas com sucesso",
                "remover" => "Aplicações removidas com sucesso",
                "substituir" => "Aplicações substituídas com sucesso",
                "aprovar" => "Solicitações aprovadas com sucesso",
                "rejeitar" => "Solicitações rejeitadas com sucesso",
                "suspender" => "Acessos suspensos com sucesso",
                "reativar" => "Acessos reativados com sucesso",
                _ => "Operação executada com sucesso"
            };

            return new ResultadoOperacao
            {
                Sucesso = erros.Count == 0,
                Mensagem = mensagem,
                Alertas = alertas,
                Erros = erros
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro em ExecutarOperacaoAplicacoes para usuário {UsuarioId}", usuario.Id);
            return new ResultadoOperacao { Sucesso = false, Mensagem = "Erro interno ao executar operação de aplicações" };
        }
    }

    private async Task<ResultadoOperacao> ProcessarAprovacaoAcesso(UsuarioAplicacao usuarioAplicacao, AprovarAcessoAplicacaoRequest request, int usuarioLogadoId)
    {
        try
        {
            var decisao = (request.Decisao ?? string.Empty).Trim().ToLower();

            switch (decisao)
            {
                case "aprovar":
                case "aprovar_temporario":
                {
                    usuarioAplicacao.Aprovado = true;
                    usuarioAplicacao.Ativo = true;
                    usuarioAplicacao.DataAprovacao = DateTime.UtcNow;
                    usuarioAplicacao.AprovadoPorId = usuarioLogadoId;
                    usuarioAplicacao.ObservacoesAprovacao = request.Observacoes;
                    if (decisao == "aprovar_temporario")
                    {
                        if (request.DuracaoTemporariaDias.HasValue)
                            usuarioAplicacao.DataExpiracao = DateTime.UtcNow.AddDays(request.DuracaoTemporariaDias.Value);
                        else if (request.DataExpiracao.HasValue)
                            usuarioAplicacao.DataExpiracao = request.DataExpiracao;
                    }
                    else if (request.DataExpiracao.HasValue)
                    {
                        usuarioAplicacao.DataExpiracao = request.DataExpiracao;
                    }
                    if (!string.IsNullOrWhiteSpace(request.ConfiguracoesUsuario))
                        usuarioAplicacao.ConfiguracoesUsuario = request.ConfiguracoesUsuario;

                    await _context.SaveChangesAsync();
                    return new ResultadoOperacao { Sucesso = true, Mensagem = "Acesso aprovado com sucesso" };
                }

                case "rejeitar":
                {
                    usuarioAplicacao.Aprovado = false;
                    usuarioAplicacao.Ativo = false;
                    usuarioAplicacao.DataAprovacao = DateTime.UtcNow;
                    usuarioAplicacao.AprovadoPorId = usuarioLogadoId;
                    usuarioAplicacao.ObservacoesAprovacao = string.IsNullOrWhiteSpace(request.Observacoes) ?
                        "Solicitação rejeitada" : request.Observacoes;
                    usuarioAplicacao.DataExpiracao = null;

                    await _context.SaveChangesAsync();
                    return new ResultadoOperacao { Sucesso = true, Mensagem = "Acesso rejeitado com sucesso" };
                }

                default:
                    return new ResultadoOperacao { Sucesso = false, Mensagem = $"Decisão inválida: {request.Decisao}" };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao processar aprovação de acesso (UsuarioId={UsuarioId}, AplicacaoId={AplicacaoId})", 
                usuarioAplicacao.UsuarioId, usuarioAplicacao.AplicacaoId);
            return new ResultadoOperacao { Sucesso = false, Mensagem = "Erro interno ao processar aprovação" };
        }
    }

    private async Task<RespostaSolicitacaoAcesso> ProcessarSolicitacaoAcesso(Usuario usuario, Aplicacao aplicacao, SolicitarAcessoAplicacaoRequest request)
    {
        try
        {
            // Verificar se já existe vínculo
            var existente = await _context.UsuariosAplicacao
                .FirstOrDefaultAsync(ua => ua.UsuarioId == usuario.Id && ua.AplicacaoId == aplicacao.Id);

            if (existente != null)
            {
                return new RespostaSolicitacaoAcesso
                {
                    Sucesso = false,
                    Mensagem = "Usuário já possui vínculo com esta aplicação",
                    SolicitacaoId = aplicacao.Id,
                    StatusSolicitacao = existente.Aprovado ? "aprovada" : "pendente",
                    RequerAprovacao = aplicacao.RequerAprovacao,
                    ProximosPassos = existente.Aprovado ? new List<string>() : new List<string> { "Aguardar análise do responsável" },
                    PrevisaoResposta = aplicacao.RequerAprovacao ? DateTime.UtcNow.AddDays(3) : DateTime.UtcNow,
                    Responsaveis = ObterResponsaveisAprovacao(aplicacao.Id)
                };
            }

            var precisaAprovacao = aplicacao.RequerAprovacao;
            var aprovadoImediato = !precisaAprovacao && aplicacao.StatusAplicacao.PermiteAcesso;

            var vinculo = new UsuarioAplicacao
            {
                UsuarioId = usuario.Id,
                AplicacaoId = aplicacao.Id,
                Justificativa = request.Justificativa,
                ConfiguracoesUsuario = request.ConfiguracoesDesejadas,
                DataSolicitacao = DateTime.UtcNow,
                Aprovado = aprovadoImediato,
                DataAprovacao = aprovadoImediato ? DateTime.UtcNow : null,
                DataExpiracao = request.DataExpiracaoDesejada,
                Ativo = true
            };

            _context.UsuariosAplicacao.Add(vinculo);
            await _context.SaveChangesAsync();

            return new RespostaSolicitacaoAcesso
            {
                Sucesso = true,
                Mensagem = aprovadoImediato ? "Acesso concedido imediatamente" : "Solicitação registrada com sucesso",
                SolicitacaoId = aplicacao.Id, // Não há ID próprio; usamos o da aplicação como referência
                StatusSolicitacao = aprovadoImediato ? "aprovada" : "pendente",
                AplicacaoSolicitada = aprovadoImediato ? await ObterAplicacaoUsuarioAsync(usuario.Id, aplicacao.Id) : null,
                RequerAprovacao = precisaAprovacao,
                ProximosPassos = aprovadoImediato ? new List<string>() : new List<string> { "Aguardar aprovação do responsável" },
                PrevisaoResposta = precisaAprovacao ? DateTime.UtcNow.AddDays(3) : DateTime.UtcNow,
                Responsaveis = ObterResponsaveisAprovacao(aplicacao.Id)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao processar solicitação de acesso (UsuarioId={UsuarioId}, AplicacaoId={AplicacaoId})", 
                usuario.Id, aplicacao.Id);
            return new RespostaSolicitacaoAcesso
            {
                Sucesso = false,
                Mensagem = "Erro interno ao criar solicitação",
                SolicitacaoId = aplicacao.Id,
                StatusSolicitacao = "erro",
                RequerAprovacao = aplicacao.RequerAprovacao,
                ProximosPassos = new List<string>(),
                Responsaveis = ObterResponsaveisAprovacao(aplicacao.Id)
            };
        }
    }

    private async Task EnviarNotificacaoAplicacoes(Usuario usuario, string operacao, ResultadoOperacao resultado)
    {
        try
        {
            if (string.IsNullOrEmpty(usuario.Email) || !usuario.NotificacaoEmail)
            {
                _logger.LogInformation("📧 Notificação não enviada (preferências ou email ausente) - Usuário: {Id}", usuario.Id);
                return;
            }

            var assunto = $"[Gestus] Alteração de acesso às suas aplicações ({operacao})";
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<h2>Atualização de Acesso</h2>");
            sb.AppendLine($"<p>Operação: <strong>{operacao}</strong></p>");
            sb.AppendLine($"<p>Status: <strong>{(resultado.Sucesso ? "Sucesso" : "Com avisos/erros")}</strong></p>");
            if (resultado.Alertas?.Any() == true)
            {
                sb.AppendLine("<h3>Avisos</h3><ul>");
                foreach (var a in resultado.Alertas) sb.AppendLine($"<li>{System.Net.WebUtility.HtmlEncode(a)}</li>");
                sb.AppendLine("</ul>");
            }
            if (resultado.Erros?.Any() == true)
            {
                sb.AppendLine("<h3>Erros</h3><ul>");
                foreach (var e in resultado.Erros) sb.AppendLine($"<li>{System.Net.WebUtility.HtmlEncode(e)}</li>");
                sb.AppendLine("</ul>");
            }

            await _emailService.EnviarEmailAsync(usuario.Email, assunto, sb.ToString(), isHtml: true);
            _logger.LogInformation("📧 Notificação enviada para {Email} sobre operação {Operacao}", usuario.Email, operacao);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao enviar notificação para {Email}", usuario.Email);
        }
    }

    private async Task EnviarNotificacaoAprovacao(Usuario usuario, Aplicacao aplicacao, AprovarAcessoAplicacaoRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(usuario.Email) || !usuario.NotificacaoEmail)
            {
                _logger.LogInformation("📧 Notificação não enviada (preferências ou email ausente) - Usuário: {Id}", usuario.Id);
                return;
            }

            var assunto = $"[Gestus] {aplicacao.Nome}: decisão da solicitação de acesso ({request.Decisao})";
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"<h2>Aplicação: {System.Net.WebUtility.HtmlEncode(aplicacao.Nome)}</h2>");
            sb.AppendLine($"<p>Decisão: <strong>{System.Net.WebUtility.HtmlEncode(request.Decisao)}</strong></p>");
            if (request.DataExpiracao.HasValue)
                sb.AppendLine($"<p>Data de expiração: {request.DataExpiracao:dd/MM/yyyy}</p>");
            if (!string.IsNullOrWhiteSpace(request.Observacoes))
                sb.AppendLine($"<p>Observações: {System.Net.WebUtility.HtmlEncode(request.Observacoes!)}</p>");

            await _emailService.EnviarEmailAsync(usuario.Email, assunto, sb.ToString(), isHtml: true);
            _logger.LogInformation("📧 Notificação de aprovação enviada para {Email} sobre aplicação {Aplicacao}", usuario.Email, aplicacao.Nome);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao enviar notificação de aprovação para {Email}", usuario.Email);
        }
    }

    private string? ObterProximaAcao(string decisao, UsuarioAplicacao usuarioAplicacao)
    {
        return (decisao ?? string.Empty).Trim().ToLower() switch
        {
            "aprovar" => "Usuário pode acessar a aplicação",
            "rejeitar" => "Usuário pode solicitar novamente após revisar justificativa",
            "aprovar_temporario" => usuarioAplicacao.DataExpiracao.HasValue 
                ? $"Acesso válido até {usuarioAplicacao.DataExpiracao:dd/MM/yyyy}" 
                : "Acesso temporário concedido",
            _ => null
        };
    }

    #endregion
}
