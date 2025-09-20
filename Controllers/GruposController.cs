using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using Gestus.Modelos;
using Gestus.Dados;
using Gestus.DTOs.Grupo;
using Gestus.DTOs.Comuns;
using Gestus.Validadores;
using System.Text;

// ✅ ALIASES para resolver ambiguidade
using UsuarioGrupoDTO = Gestus.DTOs.Grupo.UsuarioGrupo;
using UsuarioGrupoModelo = Gestus.Modelos.UsuarioGrupo;

namespace Gestus.Controllers;

/// <summary>
/// Controller para gerenciamento completo de grupos
/// Implementa operações CRUD robustas com paginação, filtros avançados e auditoria
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class GruposController : ControllerBase
{
    private readonly GestusDbContexto _context;
    private readonly ILogger<GruposController> _logger;

    public GruposController(
        GestusDbContexto context,
        ILogger<GruposController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Lista grupos com paginação e filtros
    /// </summary>
    /// <param name="filtros">Filtros de busca</param>
    /// <returns>Lista paginada de grupos</returns>
    [HttpGet]
    [ProducesResponseType(typeof(RespostaPaginada<GrupoResumo>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListarGrupos([FromQuery] FiltrosGrupo filtros)
    {
        try
        {
            if (!TemPermissao("Grupos.Listar"))
            {
                return StatusCode(403, new RespostaErro 
                { 
                    Erro = "Acesso negado", 
                    Mensagem = "Acesso negado para listar grupos" 
                });
            }

            var query = _context.Grupos.AsQueryable();

            // Aplicar filtros básicos
            if (!filtros.IncluirInativos)
                query = query.Where(g => g.Ativo);

            if (!string.IsNullOrEmpty(filtros.Nome))
                query = query.Where(g => g.Nome.Contains(filtros.Nome));

            if (!string.IsNullOrEmpty(filtros.Tipo))
                query = query.Where(g => g.Tipo == filtros.Tipo);

            if (filtros.Ativo.HasValue)
                query = query.Where(g => g.Ativo == filtros.Ativo.Value);

            // Contagem total
            var totalItens = await query.CountAsync();

            // Aplicar ordenação e paginação
            query = AplicarOrdenacao(query, filtros.OrdenarPor, filtros.DirecaoOrdenacao);
            
            var grupos = await query
                .Skip((filtros.Pagina - 1) * filtros.ItensPorPagina)
                .Take(filtros.ItensPorPagina)
                .Include(g => g.UsuarioGrupos.Where(ug => ug.Ativo))
                .Select(g => new GrupoResumo
                {
                    Id = g.Id,
                    Nome = g.Nome,
                    Descricao = g.Descricao,
                    Tipo = g.Tipo,
                    Ativo = g.Ativo,
                    DataCriacao = g.DataCriacao,
                    DataAtualizacao = g.DataAtualizacao,
                    TotalUsuarios = g.UsuarioGrupos.Count(ug => ug.Ativo),
                    UsuariosAtivos = g.UsuarioGrupos.Count(ug => ug.Ativo && ug.Usuario.Ativo)
                })
                .ToListAsync();

            var totalPaginas = (int)Math.Ceiling((double)totalItens / filtros.ItensPorPagina);

            return Ok(new RespostaPaginada<GrupoResumo>
            {
                Dados = grupos,
                PaginaAtual = filtros.Pagina,
                ItensPorPagina = filtros.ItensPorPagina,
                TotalItens = totalItens,
                TotalPaginas = totalPaginas,
                TemProximaPagina = filtros.Pagina < totalPaginas,
                TemPaginaAnterior = filtros.Pagina > 1
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar grupos");
            return StatusCode(500, new RespostaErro { Erro = "Erro interno", Mensagem = "Erro ao processar solicitação" });
        }
    }

    /// <summary>
    /// Obtém detalhes de um grupo específico
    /// </summary>
    /// <param name="id">ID do grupo</param>
    /// <returns>Detalhes completos do grupo</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(GrupoCompleto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ObterGrupo(int id)
    {
        try
        {
            if (!TemPermissao("Grupos.Visualizar"))
            {
                return StatusCode(403, new RespostaErro 
                { 
                    Erro = "Acesso negado", 
                    Mensagem = "Acesso negado para visualizar grupos" 
                });
            }

            var grupo = await _context.Grupos
                .Include(g => g.UsuarioGrupos.Where(ug => ug.Ativo))
                    .ThenInclude(ug => ug.Usuario)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (grupo == null)
            {
                return NotFound(new RespostaErro { Erro = "Não encontrado", Mensagem = "Grupo não encontrado" });
            }

            var grupoCompleto = ConstruirGrupoCompleto(grupo);
            return Ok(grupoCompleto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter grupo {GrupoId}", id);
            return StatusCode(500, new RespostaErro { Erro = "Erro interno", Mensagem = "Erro ao processar solicitação" });
        }
    }

    /// <summary>
    /// Cria um novo grupo no sistema
    /// </summary>
    /// <param name="request">Dados do novo grupo</param>
    /// <returns>Grupo criado</returns>
    [HttpPost]
    [ProducesResponseType(typeof(GrupoCompleto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CriarGrupo([FromBody] CriarGrupoRequest request)
    {
        try
        {
            if (!TemPermissao("Grupos.Criar"))
            {
                return StatusCode(403, new RespostaErro 
                { 
                    Erro = "Acesso negado", 
                    Mensagem = "Acesso negado para criar grupos" 
                });
            }

            // Validar request
            var validator = new CriarGrupoValidator(_context);
            var resultadoValidacao = await validator.ValidateAsync(request);
            
            if (!resultadoValidacao.IsValid)
            {
                return BadRequest(new RespostaErro 
                { 
                    Erro = "Dados inválidos", 
                    Mensagem = "Dados fornecidos são inválidos",
                    Detalhes = resultadoValidacao.Errors.Select(e => e.ErrorMessage).ToList()
                });
            }

            // Verificar se já existe grupo com o mesmo nome
            var grupoExistente = await _context.Grupos
                .FirstOrDefaultAsync(g => g.Nome.ToLower() == request.Nome.ToLower());

            if (grupoExistente != null)
            {
                return BadRequest(new RespostaErro 
                { 
                    Erro = "Nome duplicado", 
                    Mensagem = "Já existe um grupo com este nome" 
                });
            }

            var usuarioLogadoId = ObterUsuarioLogadoId();

            // Criar o grupo
            var novoGrupo = new Grupo
            {
                Nome = request.Nome,
                Descricao = request.Descricao,
                Tipo = request.Tipo ?? "Personalizado",
                Ativo = request.Ativo,
                DataCriacao = DateTime.UtcNow
            };

            _context.Grupos.Add(novoGrupo);
            await _context.SaveChangesAsync();

            // Adicionar usuários se especificados
            if (request.UsuariosIds?.Any() == true)
            {
                await AdicionarUsuariosAoGrupo(novoGrupo.Id, request.UsuariosIds, usuarioLogadoId);
            }

            // Registrar auditoria
            await RegistrarAuditoria("CRIAR", "Grupo", novoGrupo.Id.ToString(), 
                $"Grupo '{novoGrupo.Nome}' criado", null, novoGrupo);

            // Obter dados completos para resposta
            var grupoCompleto = await ObterGrupoCompletoAsync(novoGrupo.Id);

            _logger.LogInformation("✅ Grupo criado - ID: {GrupoId}, Nome: {Nome}, Usuário: {UsuarioId}", 
                novoGrupo.Id, novoGrupo.Nome, usuarioLogadoId);

            return CreatedAtAction(nameof(ObterGrupo), new { id = novoGrupo.Id }, grupoCompleto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar grupo");
            return StatusCode(500, new RespostaErro { Erro = "Erro interno", Mensagem = "Erro ao processar solicitação" });
        }
    }

    /// <summary>
    /// Atualiza um grupo existente
    /// </summary>
    /// <param name="id">ID do grupo a ser atualizado</param>
    /// <param name="request">Dados de atualização do grupo</param>
    /// <returns>Grupo atualizado</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(GrupoCompleto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AtualizarGrupo(int id, [FromBody] AtualizarGrupoRequest request)
    {
        try
        {
            if (!TemPermissao("Grupos.Editar"))
            {
                return StatusCode(403, new RespostaErro 
                { 
                    Erro = "Acesso negado", 
                    Mensagem = "Acesso negado para editar grupos" 
                });
            }

            // Validar request
            var validator = new AtualizarGrupoValidator(_context);
            var resultadoValidacao = await validator.ValidateAsync(request);
            
            if (!resultadoValidacao.IsValid)
            {
                return BadRequest(new RespostaErro 
                { 
                    Erro = "Dados inválidos", 
                    Mensagem = "Dados fornecidos são inválidos",
                    Detalhes = resultadoValidacao.Errors.Select(e => e.ErrorMessage).ToList()
                });
            }

            var grupo = await _context.Grupos.FindAsync(id);
            if (grupo == null)
            {
                return NotFound(new RespostaErro { Erro = "Não encontrado", Mensagem = "Grupo não encontrado" });
            }

            var dadosAntes = new
            {
                grupo.Nome,
                grupo.Descricao,
                grupo.Tipo,
                grupo.Ativo
            };

            var alterado = false;
            var usuarioLogadoId = ObterUsuarioLogadoId();

            // Verificar mudança de nome (se especificado)
            if (!string.IsNullOrEmpty(request.Nome) && request.Nome != grupo.Nome)
            {
                var grupoComNome = await _context.Grupos
                    .FirstOrDefaultAsync(g => g.Nome.ToLower() == request.Nome.ToLower() && g.Id != id);

                if (grupoComNome != null)
                {
                    return BadRequest(new RespostaErro 
                    { 
                        Erro = "Nome duplicado", 
                        Mensagem = "Já existe outro grupo com este nome" 
                    });
                }

                grupo.Nome = request.Nome;
                alterado = true;
            }

            // Aplicar outras alterações
            if (!string.IsNullOrEmpty(request.Descricao) && request.Descricao != grupo.Descricao)
            {
                grupo.Descricao = request.Descricao;
                alterado = true;
            }

            if (!string.IsNullOrEmpty(request.Tipo) && request.Tipo != grupo.Tipo)
            {
                grupo.Tipo = request.Tipo;
                alterado = true;
            }

            if (request.Ativo.HasValue && request.Ativo != grupo.Ativo)
            {
                grupo.Ativo = request.Ativo.Value;
                alterado = true;
            }

            if (alterado)
            {
                grupo.DataAtualizacao = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Registrar auditoria
                await RegistrarAuditoria("ATUALIZAR", "Grupo", grupo.Id.ToString(), 
                    $"Grupo '{grupo.Nome}' atualizado", dadosAntes, grupo);

                _logger.LogInformation("✅ Grupo atualizado - ID: {GrupoId}, Nome: {Nome}, Usuário: {UsuarioId}", 
                    grupo.Id, grupo.Nome, usuarioLogadoId);
            }

            // Obter dados completos para resposta
            var grupoCompleto = await ObterGrupoCompletoAsync(grupo.Id);
            return Ok(grupoCompleto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar grupo {GrupoId}", id);
            return StatusCode(500, new RespostaErro { Erro = "Erro interno", Mensagem = "Erro ao processar solicitação" });
        }
    }

    /// <summary>
    /// Remove um grupo do sistema (soft delete por padrão)
    /// </summary>
    /// <param name="id">ID do grupo a ser removido</param>
    /// <param name="exclusaoPermanente">Se true, faz hard delete (exclusão definitiva)</param>
    /// <returns>Confirmação da remoção</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(RespostaSucesso), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoverGrupo(int id, [FromQuery] bool exclusaoPermanente = false)
    {
        try
        {
            if (!TemPermissao("Grupos.Excluir"))
            {
                return StatusCode(403, new RespostaErro 
                { 
                    Erro = "Acesso negado", 
                    Mensagem = "Acesso negado para excluir grupos" 
                });
            }

            var grupo = await _context.Grupos
                .Include(g => g.UsuarioGrupos.Where(ug => ug.Ativo))
                .FirstOrDefaultAsync(g => g.Id == id);

            if (grupo == null)
            {
                return NotFound(new RespostaErro { Erro = "Não encontrado", Mensagem = "Grupo não encontrado" });
            }

            var usuariosAtivos = grupo.UsuarioGrupos.Count(ug => ug.Ativo);
            var usuarioLogadoId = ObterUsuarioLogadoId();

            // Validações antes da remoção
            if (exclusaoPermanente && usuariosAtivos > 0)
            {
                return BadRequest(new RespostaErro 
                { 
                    Erro = "Grupo tem usuários", 
                    Mensagem = "Não é possível fazer exclusão permanente de grupo com usuários. Remova os usuários primeiro ou use exclusão lógica." 
                });
            }

            var dadosAntes = new
            {
                grupo.Id,
                grupo.Nome,
                grupo.Descricao,
                grupo.Tipo,
                grupo.Ativo,
                TotalUsuarios = usuariosAtivos
            };

            if (exclusaoPermanente)
            {
                // Hard delete - remove completamente
                if (usuariosAtivos > 0)
                {
                    // Remover associações primeiro
                    _context.UsuarioGrupos.RemoveRange(grupo.UsuarioGrupos);
                }

                _context.Grupos.Remove(grupo);
                await _context.SaveChangesAsync();

                await RegistrarAuditoria("EXCLUIR_PERMANENTE", "Grupo", grupo.Id.ToString(), 
                    $"Grupo '{grupo.Nome}' removido permanentemente", dadosAntes, null);

                _logger.LogInformation("✅ Grupo removido permanentemente - ID: {GrupoId}, Nome: {Nome}, Usuário: {UsuarioId}", 
                    grupo.Id, grupo.Nome, usuarioLogadoId);

                return Ok(new Gestus.DTOs.Comuns.RespostaSucesso
                {
                    Mensagem = "Grupo removido permanentemente com sucesso",
                    Dados = new { GrupoId = id, TipoRemocao = "Permanente" }
                });
            }
            else
            {
                // Soft delete - apenas desativa
                grupo.Ativo = false;
                grupo.DataAtualizacao = DateTime.UtcNow;

                // Desativar associações de usuários também
                foreach (var usuarioGrupo in grupo.UsuarioGrupos.Where(ug => ug.Ativo))
                {
                    usuarioGrupo.Ativo = false;
                }

                await _context.SaveChangesAsync();

                await RegistrarAuditoria("DESATIVAR", "Grupo", grupo.Id.ToString(), 
                    $"Grupo '{grupo.Nome}' desativado", dadosAntes, grupo);

                _logger.LogInformation("✅ Grupo desativado - ID: {GrupoId}, Nome: {Nome}, Usuário: {UsuarioId}", 
                    grupo.Id, grupo.Nome, usuarioLogadoId);

                return Ok(new Gestus.DTOs.Comuns.RespostaSucesso
                {
                    Mensagem = "Grupo desativado com sucesso",
                    Dados = new { GrupoId = id, TipoRemocao = "Lógica" }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao remover grupo {GrupoId}", id);
            return StatusCode(500, new RespostaErro { Erro = "Erro interno", Mensagem = "Erro ao processar solicitação" });
        }
    }

    /// <summary>
    /// Reativa um grupo desativado
    /// </summary>
    /// <param name="id">ID do grupo a ser reativado</param>
    /// <returns>Confirmação da reativação</returns>
    [HttpPost("{id:int}/reativar")]
    [ProducesResponseType(typeof(GrupoCompleto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ReativarGrupo(int id)
    {
        try
        {
            if (!TemPermissao("Grupos.Editar"))
            {
                return StatusCode(403, new RespostaErro 
                { 
                    Erro = "Acesso negado", 
                    Mensagem = "Acesso negado para reativar grupos" 
                });
            }

            var grupo = await _context.Grupos.FindAsync(id);
            if (grupo == null)
            {
                return NotFound(new RespostaErro { Erro = "Não encontrado", Mensagem = "Grupo não encontrado" });
            }

            if (grupo.Ativo)
            {
                return BadRequest(new RespostaErro 
                { 
                    Erro = "Grupo já ativo", 
                    Mensagem = "O grupo já está ativo" 
                });
            }

            var usuarioLogadoId = ObterUsuarioLogadoId();
            var dadosAntes = new { grupo.Ativo };

            grupo.Ativo = true;
            grupo.DataAtualizacao = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await RegistrarAuditoria("REATIVAR", "Grupo", grupo.Id.ToString(), 
                $"Grupo '{grupo.Nome}' reativado", dadosAntes, grupo);

            var grupoCompleto = await ObterGrupoCompletoAsync(grupo.Id);

            _logger.LogInformation("✅ Grupo reativado - ID: {GrupoId}, Nome: {Nome}, Usuário: {UsuarioId}", 
                grupo.Id, grupo.Nome, usuarioLogadoId);

            return Ok(grupoCompleto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao reativar grupo {GrupoId}", id);
            return StatusCode(500, new RespostaErro { Erro = "Erro interno", Mensagem = "Erro ao processar solicitação" });
        }
    }

    /// <summary>
    /// Gerencia usuários de um grupo específico
    /// </summary>
    /// <param name="id">ID do grupo</param>
    /// <param name="request">Dados para gerenciamento de usuários</param>
    /// <returns>Resultado da operação</returns>
    [HttpPost("{id:int}/usuarios")]
    [ProducesResponseType(typeof(RespostaGerenciamentoUsuarios), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GerenciarUsuariosGrupo(int id, [FromBody] GerenciarUsuariosGrupoRequest request)
    {
        try
        {
            if (!TemPermissao("Grupos.GerenciarUsuarios"))
            {
                return StatusCode(403, new RespostaErro 
                { 
                    Erro = "Acesso negado", 
                    Mensagem = "Acesso negado para gerenciar usuários em grupos" 
                });
            }

            // Validar request
            var validator = new GerenciarUsuariosGrupoValidator(_context);
            var resultadoValidacao = await validator.ValidateAsync(request);
            
            if (!resultadoValidacao.IsValid)
            {
                return BadRequest(new RespostaErro 
                { 
                    Erro = "Dados inválidos", 
                    Mensagem = "Dados fornecidos são inválidos",
                    Detalhes = resultadoValidacao.Errors.Select(e => e.ErrorMessage).ToList()
                });
            }

            var grupo = await _context.Grupos.FindAsync(id);
            if (grupo == null)
            {
                return NotFound(new RespostaErro { Erro = "Não encontrado", Mensagem = "Grupo não encontrado" });
            }

            if (!grupo.Ativo)
            {
                return BadRequest(new RespostaErro 
                { 
                    Erro = "Grupo inativo", 
                    Mensagem = "Não é possível gerenciar usuários de um grupo inativo" 
                });
            }

            var usuarioLogadoId = ObterUsuarioLogadoId();
            var operacao = request.Operacao.ToLower();

            var resultado = operacao switch
            {
                "adicionar" => await ExecutarAdicaoUsuarios(id, request.UsuariosIds, usuarioLogadoId),
                "remover" => await ExecutarRemocaoUsuarios(id, request.UsuariosIds, usuarioLogadoId),
                "substituir" => await ExecutarSubstituicaoUsuarios(id, request.UsuariosIds, usuarioLogadoId),
                "limpar" => await ExecutarLimpezaUsuarios(id, usuarioLogadoId),
                _ => throw new ArgumentException($"Operação '{request.Operacao}' não é válida")
            };

            // Registrar auditoria
            await RegistrarAuditoria($"GERENCIAR_USUARIOS_{operacao.ToUpper()}", "Grupo", 
                grupo.Id.ToString(), $"Operação '{operacao}' executada no grupo '{grupo.Nome}'", 
                request, resultado);

            _logger.LogInformation("✅ Usuários do grupo gerenciados - Grupo: {GrupoId}, Operação: {Operacao}, Usuário: {UsuarioId}", 
                grupo.Id, operacao, usuarioLogadoId);

            return Ok(resultado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerenciar usuários do grupo {GrupoId}", id);
            return StatusCode(500, new RespostaErro { Erro = "Erro interno", Mensagem = "Erro ao processar solicitação" });
        }
    }

    /// <summary>
    /// Lista usuários de um grupo específico
    /// </summary>
    /// <param name="id">ID do grupo</param>
    /// <param name="filtros">Filtros para a listagem</param>
    /// <returns>Lista paginada de usuários do grupo</returns>
    [HttpGet("{id:int}/usuarios")]
    [ProducesResponseType(typeof(RespostaPaginada<UsuarioGrupoDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListarUsuariosDoGrupo(int id, [FromQuery] FiltrosUsuariosGrupo filtros)
    {
        try
        {
            if (!TemPermissao("Grupos.Visualizar"))
            {
                return StatusCode(403, new RespostaErro 
                { 
                    Erro = "Acesso negado", 
                    Mensagem = "Acesso negado para visualizar usuários do grupo" 
                });
            }

            var grupo = await _context.Grupos.FindAsync(id);
            if (grupo == null)
            {
                return NotFound(new RespostaErro { Erro = "Não encontrado", Mensagem = "Grupo não encontrado" });
            }

            var query = _context.UsuarioGrupos
                .Where(ug => ug.GrupoId == id)
                .Include(ug => ug.Usuario)
                    .ThenInclude(u => u.UsuarioPapeis.Where(up => up.Ativo))
                        .ThenInclude(up => up.Papel)
                .AsQueryable();

            // Aplicar filtros
            if (!filtros.IncluirInativos)
                query = query.Where(ug => ug.Ativo && ug.Usuario.Ativo);

            if (!string.IsNullOrEmpty(filtros.Nome))
                query = query.Where(ug => ug.Usuario.Nome.Contains(filtros.Nome) || 
                                        ug.Usuario.Sobrenome.Contains(filtros.Nome));

            if (!string.IsNullOrEmpty(filtros.Email))
                query = query.Where(ug => ug.Usuario.Email!.Contains(filtros.Email));

            if (filtros.DataAdesaoInicio.HasValue)
                query = query.Where(ug => ug.DataAdesao >= filtros.DataAdesaoInicio.Value);

            if (filtros.DataAdesaoFim.HasValue)
                query = query.Where(ug => ug.DataAdesao <= filtros.DataAdesaoFim.Value);

            // Contagem total
            var totalItens = await query.CountAsync();

            // ✅ APLICAR ORDENAÇÃO AO MODELO (não ao DTO)
            query = AplicarOrdenacaoUsuarios(query, filtros.OrdenarPor, filtros.DirecaoOrdenacao);

            // ✅ DEPOIS FAZER A PROJEÇÃO PARA DTO
            var usuariosGrupo = await query
                .Skip((filtros.Pagina - 1) * filtros.ItensPorPagina)
                .Take(filtros.ItensPorPagina)
                .Select(ug => new UsuarioGrupoDTO
                {
                    Id = ug.Usuario.Id,
                    Nome = ug.Usuario.Nome,
                    Sobrenome = ug.Usuario.Sobrenome,
                    Email = ug.Usuario.Email ?? "",
                    Ativo = ug.Usuario.Ativo,
                    DataAdesao = ug.DataAdesao,
                    Papeis = ug.Usuario.UsuarioPapeis
                        .Where(up => up.Ativo && up.Papel.Name != null)
                        .Select(up => up.Papel.Name!)
                        .ToList()
                })
                .ToListAsync();

            var totalPaginas = (int)Math.Ceiling((double)totalItens / filtros.ItensPorPagina);

            return Ok(new RespostaPaginada<UsuarioGrupoDTO>
            {
                Dados = usuariosGrupo,
                PaginaAtual = filtros.Pagina,
                ItensPorPagina = filtros.ItensPorPagina,
                TotalItens = totalItens,
                TotalPaginas = totalPaginas,
                TemProximaPagina = filtros.Pagina < totalPaginas,
                TemPaginaAnterior = filtros.Pagina > 1
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar usuários do grupo {GrupoId}", id);
            return StatusCode(500, new RespostaErro { Erro = "Erro interno", Mensagem = "Erro ao processar solicitação" });
        }
    }

    /// <summary>
    /// Obtém estatísticas detalhadas de um grupo específico
    /// </summary>
    /// <param name="id">ID do grupo</param>
    /// <param name="periodo">Período para análise: 7dias, 30dias, 90dias, 1ano, todos</param>
    /// <returns>Estatísticas detalhadas do grupo</returns>
    [HttpGet("{id:int}/estatisticas")]
    [ProducesResponseType(typeof(EstatisticasDetalhadasGrupo), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ObterEstatisticasGrupo(int id, [FromQuery] string periodo = "30dias")
    {
        try
        {
            if (!TemPermissao("Grupos.Visualizar"))
            {
                return StatusCode(403, new RespostaErro 
                { 
                    Erro = "Acesso negado", 
                    Mensagem = "Acesso negado para visualizar estatísticas de grupos" 
                });
            }

            var grupo = await _context.Grupos.FindAsync(id);
            if (grupo == null)
            {
                return NotFound(new RespostaErro { Erro = "Não encontrado", Mensagem = "Grupo não encontrado" });
            }

            var dataInicio = CalcularDataInicioPeriodo(periodo);
            var estatisticas = await GerarEstatisticasDetalhadas(id, dataInicio);

            return Ok(estatisticas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter estatísticas do grupo {GrupoId}", id);
            return StatusCode(500, new RespostaErro { Erro = "Erro interno", Mensagem = "Erro ao processar solicitação" });
        }
    }

    /// <summary>
    /// Busca avançada de grupos com filtros complexos
    /// </summary>
    /// <param name="request">Critérios de busca avançada</param>
    /// <returns>Resultados da busca</returns>
    [HttpPost("buscar")]
    [ProducesResponseType(typeof(RespostaBuscaAvancadaGrupos), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> BuscarGrupos([FromBody] BuscaAvancadaGruposRequest request)
    {
        try
        {
            if (!TemPermissao("Grupos.Listar"))
            {
                return StatusCode(403, new RespostaErro 
                { 
                    Erro = "Acesso negado", 
                    Mensagem = "Acesso negado para buscar grupos" 
                });
            }

            // Validar request
            var validator = new BuscaAvancadaGruposValidator();
            var resultadoValidacao = await validator.ValidateAsync(request);
            
            if (!resultadoValidacao.IsValid)
            {
                return BadRequest(new RespostaErro 
                { 
                    Erro = "Dados inválidos", 
                    Mensagem = "Critérios de busca inválidos",
                    Detalhes = resultadoValidacao.Errors.Select(e => e.ErrorMessage).ToList()
                });
            }

            var query = _context.Grupos.AsQueryable();

            // Aplicar filtros de busca avançada
            query = AplicarFiltrosBuscaAvancada(query, request);

            // Aplicar ordenação
            query = AplicarOrdenacao(query, request.OrdenarPor, request.DirecaoOrdenacao);

            // Contagem total
            var totalItens = await query.CountAsync();

            // Buscar grupos com paginação
            var grupos = await query
                .Skip((request.Pagina - 1) * request.ItensPorPagina)
                .Take(request.ItensPorPagina)
                .Include(g => g.UsuarioGrupos.Where(ug => ug.Ativo))
                    .ThenInclude(ug => ug.Usuario)
                .Select(g => new GrupoDetalhado
                {
                    Id = g.Id,
                    Nome = g.Nome,
                    Descricao = g.Descricao,
                    Tipo = g.Tipo,
                    Ativo = g.Ativo,
                    DataCriacao = g.DataCriacao,
                    DataAtualizacao = g.DataAtualizacao,
                    TotalUsuarios = g.UsuarioGrupos.Count(ug => ug.Ativo),
                    UsuariosAtivos = g.UsuarioGrupos.Count(ug => ug.Ativo && ug.Usuario.Ativo),
                    UltimaAdesao = g.UsuarioGrupos.Where(ug => ug.Ativo).Max(ug => (DateTime?)ug.DataAdesao),
                    DistribuicaoPorPapel = g.UsuarioGrupos
                        .Where(ug => ug.Ativo && ug.Usuario.Ativo)
                        .SelectMany(ug => ug.Usuario.UsuarioPapeis.Where(up => up.Ativo && up.Papel.Name != null))
                        .GroupBy(up => up.Papel.Name!)
                        .ToDictionary(grp => grp.Key, grp => grp.Count())
                })
                .ToListAsync();

            var totalPaginas = (int)Math.Ceiling((double)totalItens / request.ItensPorPagina);

            // Gerar sugestões se poucos resultados
            var sugestoes = new List<string>();
            if (totalItens < 3 && !string.IsNullOrEmpty(request.TermoBusca))
            {
                sugestoes = await GerarSugestoesBusca(request.TermoBusca);
            }

            return Ok(new RespostaBuscaAvancadaGrupos
            {
                Grupos = grupos,
                TotalItens = totalItens,
                TotalPaginas = totalPaginas,
                PaginaAtual = request.Pagina,
                ItensPorPagina = request.ItensPorPagina,
                TemProximaPagina = request.Pagina < totalPaginas,
                TemPaginaAnterior = request.Pagina > 1,
                Sugestoes = sugestoes,
                CriteriosAplicados = GerarResumoFiltros(request)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao executar busca avançada de grupos");
            return StatusCode(500, new RespostaErro { Erro = "Erro interno", Mensagem = "Erro ao processar solicitação" });
        }
    }

    /// <summary>
    /// Exporta dados de grupos para diferentes formatos
    /// </summary>
    /// <param name="request">Configurações de exportação</param>
    /// <returns>Arquivo de exportação</returns>
    [HttpPost("exportar")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportarGrupos([FromBody] ExportarGruposRequest request)
    {
        try
        {
            if (!TemPermissao("Grupos.Exportar"))
            {
                return StatusCode(403, new RespostaErro 
                { 
                    Erro = "Acesso negado", 
                    Mensagem = "Acesso negado para exportar dados de grupos" 
                });
            }

            // Validar request
            var validator = new ExportarGruposValidator();
            var resultadoValidacao = await validator.ValidateAsync(request);
            
            if (!resultadoValidacao.IsValid)
            {
                return BadRequest(new RespostaErro 
                { 
                    Erro = "Dados inválidos", 
                    Mensagem = "Configurações de exportação inválidas",
                    Detalhes = resultadoValidacao.Errors.Select(e => e.ErrorMessage).ToList()
                });
            }

            var query = _context.Grupos.AsQueryable();

            // Aplicar filtros se especificados
            if (request.Filtros != null)
            {
                query = AplicarFiltrosExportacao(query, request.Filtros);
            }

            // Buscar dados para exportação
            var grupos = await query
                .Include(g => g.UsuarioGrupos.Where(ug => ug.Ativo))
                    .ThenInclude(ug => ug.Usuario)
                .ToListAsync();

            var dadosExportacao = await PrepararDadosExportacao(grupos, request);

            // Gerar arquivo baseado no formato
            var arquivo = request.Formato.ToLower() switch
            {
                "csv" => GerarArquivoCSV(dadosExportacao),
                "xlsx" => GerarArquivoExcel(dadosExportacao),
                "json" => GerarArquivoJSON(dadosExportacao),
                "pdf" => await GerarArquivoPDF(dadosExportacao),
                _ => throw new ArgumentException($"Formato '{request.Formato}' não suportado")
            };

            var nomeArquivo = $"grupos_export_{DateTime.Now:yyyyMMdd_HHmmss}.{request.Formato.ToLower()}";
            var contentType = ObterContentType(request.Formato);

            // Registrar auditoria
            await RegistrarAuditoria("EXPORTAR", "Grupos", null, 
                $"Exportação de {grupos.Count} grupos em formato {request.Formato}", 
                request, new { TotalGrupos = grupos.Count, Formato = request.Formato });

            return File(arquivo, contentType, nomeArquivo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao exportar grupos");
            return StatusCode(500, new RespostaErro { Erro = "Erro interno", Mensagem = "Erro ao processar solicitação" });
        }
    }

    /// <summary>
    /// Obtém relatório de grupos de um usuário específico
    /// </summary>
    /// <param name="usuarioId">ID do usuário</param>
    /// <param name="incluirHistorico">Incluir histórico de grupos anteriores</param>
    /// <returns>Relatório de grupos do usuário</returns>
    [HttpGet("usuario/{usuarioId:int}")]
    [ProducesResponseType(typeof(RelatorioGruposUsuario), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ObterGruposDoUsuario(int usuarioId, [FromQuery] bool incluirHistorico = false)
    {
        try
        {
            if (!TemPermissao("Grupos.Visualizar"))
            {
                return StatusCode(403, new RespostaErro 
                { 
                    Erro = "Acesso negado", 
                    Mensagem = "Acesso negado para visualizar grupos de usuários" 
                });
            }

            var usuario = await _context.Users.FindAsync(usuarioId);
            if (usuario == null)
            {
                return NotFound(new RespostaErro { Erro = "Não encontrado", Mensagem = "Usuário não encontrado" });
            }

            var query = _context.UsuarioGrupos
                .Where(ug => ug.UsuarioId == usuarioId)
                .Include(ug => ug.Grupo);

            if (!incluirHistorico)
            {
                query = (Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Modelos.UsuarioGrupo, Grupo>)query.Where(ug => ug.Ativo);
            }

            var gruposUsuario = await query
                .OrderByDescending(ug => ug.DataAdesao)
                .Select(ug => new GrupoDoUsuario
                {
                    GrupoId = ug.GrupoId,
                    Nome = ug.Grupo.Nome,
                    Descricao = ug.Grupo.Descricao,
                    Tipo = ug.Grupo.Tipo,
                    DataAdesao = ug.DataAdesao,
                    Ativo = ug.Ativo,
                    GrupoAtivo = ug.Grupo.Ativo,
                    TotalMembrosGrupo = ug.Grupo.UsuarioGrupos.Count(ugCount => ugCount.Ativo)
                })
                .ToListAsync();

            var estatisticas = new EstatisticasGruposUsuario
            {
                TotalGrupos = gruposUsuario.Count,
                GruposAtivos = gruposUsuario.Count(g => g.Ativo && g.GrupoAtivo),
                GruposInativos = gruposUsuario.Count(g => !g.Ativo || !g.GrupoAtivo),
                PrimeiraAdesao = gruposUsuario.MinBy(g => g.DataAdesao)?.DataAdesao,
                UltimaAdesao = gruposUsuario.MaxBy(g => g.DataAdesao)?.DataAdesao,
                DistribuicaoPorTipo = gruposUsuario
                    .Where(g => !string.IsNullOrEmpty(g.Tipo))
                    .GroupBy(g => g.Tipo!)
                    .ToDictionary(grp => grp.Key, grp => grp.Count())
            };

            return Ok(new RelatorioGruposUsuario
            {
                Usuario = new { Id = usuario.Id, Nome = usuario.Nome, Sobrenome = usuario.Sobrenome, Email = usuario.Email },
                Grupos = gruposUsuario,
                Estatisticas = estatisticas,
                DataGeracao = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter grupos do usuário {UsuarioId}", usuarioId);
            return StatusCode(500, new RespostaErro { Erro = "Erro interno", Mensagem = "Erro ao processar solicitação" });
        }
    }

    #region Métodos Auxiliares Básicos

    /// <summary>
    /// Verifica se o usuário tem permissão específica
    /// </summary>
    private bool TemPermissao(string permissao)
    {
        try
        {
            var permissaoClaim = User.FindAll("permissao").Any(c => c.Value == permissao);
            if (permissaoClaim) return true;

            var isSuperAdmin = User.IsInRole("SuperAdmin");
            if (isSuperAdmin) return true;

            var hasSystemControl = User.FindAll("permissao").Any(c => c.Value == "Sistema.Controle.Total");
            return hasSystemControl;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Aplica ordenação à query
    /// </summary>
    private IQueryable<Grupo> AplicarOrdenacao(IQueryable<Grupo> query, string? ordenarPor, string? direcao)
    {
        var crescente = string.IsNullOrEmpty(direcao) || direcao.ToLower() == "asc";
        
        return (ordenarPor?.ToLower()) switch
        {
            "nome" => crescente ? query.OrderBy(g => g.Nome) : query.OrderByDescending(g => g.Nome),
            "tipo" => crescente ? query.OrderBy(g => g.Tipo) : query.OrderByDescending(g => g.Tipo),
            "ativo" => crescente ? query.OrderBy(g => g.Ativo) : query.OrderByDescending(g => g.Ativo),
            _ => crescente ? query.OrderBy(g => g.DataCriacao) : query.OrderByDescending(g => g.DataCriacao)
        };
    }

    /// <summary>
    /// Constrói objeto GrupoCompleto
    /// </summary>
    private GrupoCompleto ConstruirGrupoCompleto(Grupo grupo)
    {
        var usuariosAtivos = grupo.UsuarioGrupos.Where(ug => ug.Ativo && ug.Usuario.Ativo).Count();
        
        return new GrupoCompleto
        {
            Id = grupo.Id,
            Nome = grupo.Nome,
            Descricao = grupo.Descricao,
            Tipo = grupo.Tipo,
            Ativo = grupo.Ativo,
            DataCriacao = grupo.DataCriacao,
            DataAtualizacao = grupo.DataAtualizacao,
            TotalUsuarios = grupo.UsuarioGrupos.Count(ug => ug.Ativo),
            Estatisticas = new EstatisticasGrupo
            {
                TotalUsuarios = grupo.UsuarioGrupos.Count(ug => ug.Ativo),
                UsuariosAtivos = usuariosAtivos,
                UsuariosInativos = grupo.UsuarioGrupos.Count(ug => ug.Ativo) - usuariosAtivos,
                UltimaAdesao = grupo.UsuarioGrupos.Where(ug => ug.Ativo).Max(ug => (DateTime?)ug.DataAdesao),
                PrimeiraAdesao = grupo.UsuarioGrupos.Where(ug => ug.Ativo).Min(ug => (DateTime?)ug.DataAdesao)
            },
            Usuarios = grupo.UsuarioGrupos.Where(ug => ug.Ativo).Select(ug => new UsuarioGrupoDTO // ✅ USANDO ALIAS
            {
                Id = ug.Usuario.Id,
                Nome = ug.Usuario.Nome,
                Sobrenome = ug.Usuario.Sobrenome,
                Email = ug.Usuario.Email ?? "",
                Ativo = ug.Usuario.Ativo,
                DataAdesao = ug.DataAdesao
            }).ToList()
        };
    }

    #endregion

    #region Métodos Auxiliares Avançados

    /// <summary>
    /// Obtém ID do usuário logado
    /// </summary>
    private int ObterUsuarioLogadoId()
    {
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("user_id") ?? User.FindFirst("id");
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
        {
            return userId;
        }

        _logger.LogWarning("⚠️ Não foi possível obter ID do usuário logado");
        return 0;
    }

    /// <summary>
    /// Adiciona usuários ao grupo
    /// </summary>
    private async Task AdicionarUsuariosAoGrupo(int grupoId, List<int> usuariosIds, int atribuidoPorId)
    {
        var usuariosValidos = await _context.Users
            .Where(u => usuariosIds.Contains(u.Id) && u.Ativo)
            .Select(u => u.Id)
            .ToListAsync();

        foreach (var usuarioIdValido in usuariosValidos) // ✅ CORRIGIDO: mudei de usuarioId para usuarioIdValido
        {
            var associacaoExistente = await _context.UsuarioGrupos
                .FirstOrDefaultAsync(ug => ug.UsuarioId == usuarioIdValido && ug.GrupoId == grupoId); // ✅ CORRIGIDO

            if (associacaoExistente == null)
            {
                _context.UsuarioGrupos.Add(new UsuarioGrupoModelo
                {
                    UsuarioId = usuarioIdValido, // ✅ CORRIGIDO
                    GrupoId = grupoId,
                    DataAdesao = DateTime.UtcNow,
                    Ativo = true
                });
            }
            else if (!associacaoExistente.Ativo)
            {
                associacaoExistente.Ativo = true;
                associacaoExistente.DataAdesao = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Aplica ordenação à query de usuários do grupo
    /// </summary>
    private IQueryable<UsuarioGrupoModelo> AplicarOrdenacaoUsuarios(IQueryable<UsuarioGrupoModelo> query, string? ordenarPor, string? direcao)
    {
        var crescente = string.IsNullOrEmpty(direcao) || direcao.ToLower() == "asc";
        
        return (ordenarPor?.ToLower()) switch
        {
            "nome" => crescente ? query.OrderBy(ug => ug.Usuario.Nome) : query.OrderByDescending(ug => ug.Usuario.Nome),
            "email" => crescente ? query.OrderBy(ug => ug.Usuario.Email) : query.OrderByDescending(ug => ug.Usuario.Email),
            "ativo" => crescente ? query.OrderBy(ug => ug.Usuario.Ativo) : query.OrderByDescending(ug => ug.Usuario.Ativo),
            "dataadesao" or _ => crescente ? query.OrderBy(ug => ug.DataAdesao) : query.OrderByDescending(ug => ug.DataAdesao)
        };
    }

    /// <summary>
    /// Obtém grupo completo com dados relacionados
    /// </summary>
    private async Task<GrupoCompleto> ObterGrupoCompletoAsync(int grupoId)
    {
        var grupo = await _context.Grupos
            .Include(g => g.UsuarioGrupos.Where(ug => ug.Ativo))
                .ThenInclude(ug => ug.Usuario)
            .FirstOrDefaultAsync(g => g.Id == grupoId);

        if (grupo == null)
        {
            throw new InvalidOperationException($"Grupo com ID {grupoId} não encontrado");
        }

        return ConstruirGrupoCompleto(grupo);
    }

    /// <summary>
    /// Registra ação na auditoria
    /// </summary>
    private async Task RegistrarAuditoria(string acao, string recurso, string? recursoId, 
        string observacoes, object? dadosAntes = null, object? dadosDepois = null)
    {
        var usuarioLogadoId = ObterUsuarioLogadoId();
        
        var registro = new RegistroAuditoria
        {
            UsuarioId = usuarioLogadoId > 0 ? usuarioLogadoId : null,
            Acao = acao,
            Recurso = recurso,
            RecursoId = recursoId,
            Observacoes = observacoes,
            DataHora = DateTime.UtcNow,
            EnderecoIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = HttpContext.Request.Headers.UserAgent.ToString(),
            DadosAntes = dadosAntes != null ? System.Text.Json.JsonSerializer.Serialize(dadosAntes) : null,
            DadosDepois = dadosDepois != null ? System.Text.Json.JsonSerializer.Serialize(dadosDepois) : null
        };

        _context.RegistrosAuditoria.Add(registro);
        await _context.SaveChangesAsync();
    }

    #endregion

    #region Métodos Auxiliares de Gerenciamento de Usuários

    /// <summary>
    /// Executa adição de usuários ao grupo
    /// </summary>
    private async Task<RespostaGerenciamentoUsuarios> ExecutarAdicaoUsuarios(int grupoId, List<int> usuariosIds, int usuarioLogadoId)
    {
        var usuariosValidos = await _context.Users
            .Where(u => usuariosIds.Contains(u.Id) && u.Ativo)
            .ToListAsync();

        var usuariosAdicionados = 0;
        var detalhes = new List<string>();

        foreach (var usuario in usuariosValidos)
        {
            var associacaoExistente = await _context.UsuarioGrupos
                .FirstOrDefaultAsync(ug => ug.UsuarioId == usuario.Id && ug.GrupoId == grupoId);

            if (associacaoExistente == null)
            {
                _context.UsuarioGrupos.Add(new UsuarioGrupoModelo
                {
                    UsuarioId = usuario.Id,
                    GrupoId = grupoId,
                    DataAdesao = DateTime.UtcNow,
                    Ativo = true
                });
                usuariosAdicionados++;
                detalhes.Add($"Usuário '{usuario.Nome} {usuario.Sobrenome}' adicionado ao grupo");
            }
            else if (!associacaoExistente.Ativo)
            {
                associacaoExistente.Ativo = true;
                associacaoExistente.DataAdesao = DateTime.UtcNow;
                usuariosAdicionados++;
                detalhes.Add($"Usuário '{usuario.Nome} {usuario.Sobrenome}' reativado no grupo");
            }
            else
            {
                detalhes.Add($"Usuário '{usuario.Nome} {usuario.Sobrenome}' já está no grupo");
            }
        }

        await _context.SaveChangesAsync();

        var totalUsuarios = await _context.UsuarioGrupos
            .CountAsync(ug => ug.GrupoId == grupoId && ug.Ativo);

        return new RespostaGerenciamentoUsuarios
        {
            Sucesso = true,
            Mensagem = $"{usuariosAdicionados} usuário(s) adicionado(s) ao grupo",
            TotalUsuarios = totalUsuarios,
            UsuariosAdicionados = usuariosAdicionados,
            UsuariosRemovidos = 0,
            Detalhes = detalhes
        };
    }

    /// <summary>
    /// Executa remoção de usuários do grupo
    /// </summary>
    private async Task<RespostaGerenciamentoUsuarios> ExecutarRemocaoUsuarios(int grupoId, List<int> usuariosIds, int usuarioLogadoId)
    {
        var associacoes = await _context.UsuarioGrupos
            .Include(ug => ug.Usuario)
            .Where(ug => ug.GrupoId == grupoId && usuariosIds.Contains(ug.UsuarioId) && ug.Ativo)
            .ToListAsync();

        var usuariosRemovidos = 0;
        var detalhes = new List<string>();

        foreach (var associacao in associacoes)
        {
            associacao.Ativo = false;
            usuariosRemovidos++;
            detalhes.Add($"Usuário '{associacao.Usuario.Nome} {associacao.Usuario.Sobrenome}' removido do grupo");
        }

        await _context.SaveChangesAsync();

        var totalUsuarios = await _context.UsuarioGrupos
            .CountAsync(ug => ug.GrupoId == grupoId && ug.Ativo);

        return new RespostaGerenciamentoUsuarios
        {
            Sucesso = true,
            Mensagem = $"{usuariosRemovidos} usuário(s) removido(s) do grupo",
            TotalUsuarios = totalUsuarios,
            UsuariosAdicionados = 0,
            UsuariosRemovidos = usuariosRemovidos,
            Detalhes = detalhes
        };
    }

    /// <summary>
    /// Executa substituição completa de usuários do grupo
    /// </summary>
    private async Task<RespostaGerenciamentoUsuarios> ExecutarSubstituicaoUsuarios(int grupoId, List<int> usuariosIds, int usuarioLogadoId)
    {
        // Primeiro, desativar todos os usuários atuais
        var usuariosAtuais = await _context.UsuarioGrupos
            .Where(ug => ug.GrupoId == grupoId && ug.Ativo)
            .ToListAsync();

        foreach (var associacao in usuariosAtuais)
        {
            associacao.Ativo = false;
        }

        // Depois, adicionar os novos usuários
        var resultadoAdicao = await ExecutarAdicaoUsuarios(grupoId, usuariosIds, usuarioLogadoId);

        return new RespostaGerenciamentoUsuarios
        {
            Sucesso = true,
            Mensagem = $"Usuários do grupo substituídos: {usuariosAtuais.Count} removidos, {resultadoAdicao.UsuariosAdicionados} adicionados",
            TotalUsuarios = resultadoAdicao.TotalUsuarios,
            UsuariosAdicionados = resultadoAdicao.UsuariosAdicionados,
            UsuariosRemovidos = usuariosAtuais.Count,
            Detalhes = new List<string>
            {
                $"{usuariosAtuais.Count} usuário(s) removido(s)",
                $"{resultadoAdicao.UsuariosAdicionados} usuário(s) adicionado(s)"
            }
        };
    }

    /// <summary>
    /// Executa limpeza (remoção de todos os usuários) do grupo
    /// </summary>
    private async Task<RespostaGerenciamentoUsuarios> ExecutarLimpezaUsuarios(int grupoId, int usuarioLogadoId)
    {
        var usuariosAtuais = await _context.UsuarioGrupos
            .Include(ug => ug.Usuario)
            .Where(ug => ug.GrupoId == grupoId && ug.Ativo)
            .ToListAsync();

        var usuariosRemovidos = 0;
        var detalhes = new List<string>();

        foreach (var associacao in usuariosAtuais)
        {
            associacao.Ativo = false;
            usuariosRemovidos++;
            detalhes.Add($"Usuário '{associacao.Usuario.Nome} {associacao.Usuario.Sobrenome}' removido do grupo");
        }

        await _context.SaveChangesAsync();

        return new RespostaGerenciamentoUsuarios
        {
            Sucesso = true,
            Mensagem = $"Todos os usuários ({usuariosRemovidos}) foram removidos do grupo",
            TotalUsuarios = 0,
            UsuariosAdicionados = 0,
            UsuariosRemovidos = usuariosRemovidos,
            Detalhes = detalhes
        };
    }

    /// <summary>
    /// Aplica ordenação à query de usuários do grupo
    /// </summary>
    private IQueryable<DTOs.Grupo.UsuarioGrupo> AplicarOrdenacaoUsuarios(IQueryable<DTOs.Grupo.UsuarioGrupo> query, string? ordenarPor, string? direcao)
    {
        var crescente = string.IsNullOrEmpty(direcao) || direcao.ToLower() == "asc";
        
        return (ordenarPor?.ToLower()) switch
        {
            "nome" => crescente ? query.OrderBy(ug => ug.Nome) : query.OrderByDescending(ug => ug.Nome),
            "email" => crescente ? query.OrderBy(ug => ug.Email) : query.OrderByDescending(ug => ug.Email),
            "ativo" => crescente ? query.OrderBy(ug => ug.Ativo) : query.OrderByDescending(ug => ug.Ativo),
            "dataadesao" or _ => crescente ? query.OrderBy(ug => ug.DataAdesao) : query.OrderByDescending(ug => ug.DataAdesao)
        };
    }

    #endregion

    #region Métodos Auxiliares de Estatísticas e Relatórios

    /// <summary>
    /// Calcula data de início baseada no período
    /// </summary>
    private DateTime? CalcularDataInicioPeriodo(string periodo)
    {
        return periodo.ToLower() switch
        {
            "7dias" => DateTime.UtcNow.AddDays(-7),
            "30dias" => DateTime.UtcNow.AddDays(-30),
            "90dias" => DateTime.UtcNow.AddDays(-90),
            "1ano" => DateTime.UtcNow.AddYears(-1),
            _ => null
        };
    }

    /// <summary>
    /// Gera estatísticas detalhadas do grupo
    /// </summary>
    private async Task<EstatisticasDetalhadasGrupo> GerarEstatisticasDetalhadas(int grupoId, DateTime? dataInicio)
    {
        var grupoQuery = _context.UsuarioGrupos
            .Where(ug => ug.GrupoId == grupoId);

        if (dataInicio.HasValue)
        {
            grupoQuery = grupoQuery.Where(ug => ug.DataAdesao >= dataInicio.Value);
        }

        var totalMembros = await grupoQuery.CountAsync(ug => ug.Ativo);
        var membrosAtivos = await grupoQuery.CountAsync(ug => ug.Ativo && ug.Usuario.Ativo);
        var novosMembros = dataInicio.HasValue ? 
            await grupoQuery.CountAsync(ug => ug.DataAdesao >= dataInicio.Value && ug.Ativo) : 0;

        var crescimento = await CalcularTaxaCrescimento(grupoId, dataInicio ?? DateTime.UtcNow.AddDays(-30));

        var distribuicaoPorPapel = await _context.UsuarioGrupos
            .Where(ug => ug.GrupoId == grupoId && ug.Ativo)
            .SelectMany(ug => ug.Usuario.UsuarioPapeis.Where(up => up.Ativo && up.Papel.Name != null))
            .GroupBy(up => up.Papel.Name!)
            .Select(grp => new { Papel = grp.Key, Quantidade = grp.Count() })
            .ToDictionaryAsync(x => x.Papel, x => x.Quantidade);

        return new EstatisticasDetalhadasGrupo
        {
            TotalMembros = totalMembros,
            MembrosAtivos = membrosAtivos,
            MembrosInativos = totalMembros - membrosAtivos,
            NovosMembros = novosMembros,
            TaxaCrescimento = crescimento,
            DistribuicaoPorPapel = distribuicaoPorPapel,
            Periodo = dataInicio?.ToString("yyyy-MM-dd") ?? "Todos os tempos"
        };
    }

    /// <summary>
    /// Aplica filtros de busca avançada
    /// </summary>
    private IQueryable<Grupo> AplicarFiltrosBuscaAvancada(IQueryable<Grupo> query, BuscaAvancadaGruposRequest request)
    {
        if (!string.IsNullOrEmpty(request.TermoBusca))
        {
            query = query.Where(g => g.Nome.Contains(request.TermoBusca) || 
                                    g.Descricao.Contains(request.TermoBusca));
        }

        if (request.Tipos?.Any() == true)
        {
            query = query.Where(g => g.Tipo != null && request.Tipos.Contains(g.Tipo));
        }

        if (request.Ativo.HasValue)
        {
            query = query.Where(g => g.Ativo == request.Ativo.Value);
        }

        if (request.DataCriacaoInicio.HasValue)
        {
            query = query.Where(g => g.DataCriacao >= request.DataCriacaoInicio.Value);
        }

        if (request.DataCriacaoFim.HasValue)
        {
            query = query.Where(g => g.DataCriacao <= request.DataCriacaoFim.Value);
        }

        if (request.MinUsuarios.HasValue)
        {
            query = query.Where(g => g.UsuarioGrupos.Count(ug => ug.Ativo) >= request.MinUsuarios.Value);
        }

        if (request.MaxUsuarios.HasValue)
        {
            query = query.Where(g => g.UsuarioGrupos.Count(ug => ug.Ativo) <= request.MaxUsuarios.Value);
        }

        return query;
    }

    /// <summary>
    /// Gera sugestões de busca
    /// </summary>
    private async Task<List<string>> GerarSugestoesBusca(string termo)
    {
        var sugestoes = new List<string>();

        // Buscar nomes similares
        var nomesSimilares = await _context.Grupos
            .Where(g => g.Nome.Contains(termo.Substring(0, Math.Min(termo.Length, 3))))
            .Select(g => g.Nome)
            .Take(3)
            .ToListAsync();

        sugestoes.AddRange(nomesSimilares);

        // Buscar tipos similares
        var tiposSimilares = await _context.Grupos
            .Where(g => !string.IsNullOrEmpty(g.Tipo) && g.Tipo.Contains(termo))
            .Select(g => g.Tipo!)
            .Distinct()
            .Take(2)
            .ToListAsync();

        sugestoes.AddRange(tiposSimilares);

        return sugestoes.Distinct().ToList();
    }

    /// <summary>
    /// Calcula taxa de crescimento do grupo
    /// </summary>
    private async Task<decimal> CalcularTaxaCrescimento(int grupoId, DateTime dataReferencia)
    {
        var membrosAtual = await _context.UsuarioGrupos
            .CountAsync(ug => ug.GrupoId == grupoId && ug.Ativo);

        var membrosAnterior = await _context.UsuarioGrupos
            .CountAsync(ug => ug.GrupoId == grupoId && ug.DataAdesao < dataReferencia && ug.Ativo);

        if (membrosAnterior == 0) return membrosAtual > 0 ? 100 : 0;

        return ((decimal)(membrosAtual - membrosAnterior) / membrosAnterior) * 100;
    }

    #endregion

    #region Métodos Auxiliares de Exportação

    /// <summary>
    /// Aplica filtros para exportação
    /// </summary>
    private IQueryable<Grupo> AplicarFiltrosExportacao(IQueryable<Grupo> query, FiltrosGrupo filtros)
    {
        if (!filtros.IncluirInativos)
            query = query.Where(g => g.Ativo);

        if (!string.IsNullOrEmpty(filtros.Nome))
            query = query.Where(g => g.Nome.Contains(filtros.Nome));

        if (!string.IsNullOrEmpty(filtros.Tipo))
            query = query.Where(g => g.Tipo == filtros.Tipo);

        if (filtros.Ativo.HasValue)
            query = query.Where(g => g.Ativo == filtros.Ativo.Value);

        if (filtros.DataCriacaoInicio.HasValue)
            query = query.Where(g => g.DataCriacao >= filtros.DataCriacaoInicio.Value);

        if (filtros.DataCriacaoFim.HasValue)
            query = query.Where(g => g.DataCriacao <= filtros.DataCriacaoFim.Value);

        if (filtros.MinUsuarios.HasValue)
            query = query.Where(g => g.UsuarioGrupos.Count(ug => ug.Ativo) >= filtros.MinUsuarios.Value);

        if (filtros.MaxUsuarios.HasValue)
            query = query.Where(g => g.UsuarioGrupos.Count(ug => ug.Ativo) <= filtros.MaxUsuarios.Value);

        return query;
    }

    /// <summary>
    /// Prepara dados para exportação
    /// </summary>
    private async Task<List<DadosExportacaoGrupo>> PrepararDadosExportacao(List<Grupo> grupos, ExportarGruposRequest request)
    {
        return await Task.Run(() =>
        {
            var dados = new List<DadosExportacaoGrupo>();

            foreach (var grupo in grupos)
            {
                var usuariosAtivos = grupo.UsuarioGrupos.Count(ug => ug.Ativo && ug.Usuario.Ativo);
                var totalUsuarios = grupo.UsuarioGrupos.Count(ug => ug.Ativo);

                var dadosGrupo = new DadosExportacaoGrupo
                {
                    Id = grupo.Id,
                    Nome = grupo.Nome,
                    Descricao = grupo.Descricao,
                    Tipo = grupo.Tipo ?? "Sem tipo",
                    Ativo = grupo.Ativo ? "Sim" : "Não",
                    DataCriacao = grupo.DataCriacao.ToString("dd/MM/yyyy HH:mm"),
                    DataAtualizacao = grupo.DataAtualizacao?.ToString("dd/MM/yyyy HH:mm") ?? "Nunca",
                    TotalUsuarios = totalUsuarios,
                    UsuariosAtivos = usuariosAtivos,
                    UsuariosInativos = totalUsuarios - usuariosAtivos
                };

                // Incluir detalhes se solicitado
                if (request.IncluirDetalhes)
                {
                    dadosGrupo.Usuarios = grupo.UsuarioGrupos
                        .Where(ug => ug.Ativo)
                        .Select(ug => new
                        {
                            Nome = $"{ug.Usuario.Nome} {ug.Usuario.Sobrenome}",
                            Email = ug.Usuario.Email,
                            Ativo = ug.Usuario.Ativo ? "Sim" : "Não",
                            DataAdesao = ug.DataAdesao.ToString("dd/MM/yyyy")
                        })
                        .ToList();
                }

                dados.Add(dadosGrupo);
            }

            return dados;
        });
    }

    /// <summary>
    /// Gera arquivo CSV
    /// </summary>
    private byte[] GerarArquivoCSV(List<DadosExportacaoGrupo> dados)
    {
        var csv = new StringBuilder();
        
        // Cabeçalho
        csv.AppendLine("Id,Nome,Descrição,Tipo,Ativo,Data Criação,Data Atualização,Total Usuários,Usuários Ativos,Usuários Inativos");

        // Dados
        foreach (var item in dados)
        {
            csv.AppendLine($"{item.Id},\"{item.Nome}\",\"{item.Descricao}\",\"{item.Tipo}\",{item.Ativo},{item.DataCriacao},{item.DataAtualizacao},{item.TotalUsuarios},{item.UsuariosAtivos},{item.UsuariosInativos}");
        }

        return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
    }

    /// <summary>
    /// Gera arquivo Excel
    /// </summary>
    private byte[] GerarArquivoExcel(List<DadosExportacaoGrupo> dados)
    {
        // Implementação básica - seria ideal usar EPPlus ou similar
        return GerarArquivoCSV(dados); // Fallback para CSV por simplicidade
    }

    /// <summary>
    /// Gera arquivo JSON
    /// </summary>
    private byte[] GerarArquivoJSON(List<DadosExportacaoGrupo> dados)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(dados, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return System.Text.Encoding.UTF8.GetBytes(json);
    }

    /// <summary>
    /// Gera arquivo PDF
    /// </summary>
    private async Task<byte[]> GerarArquivoPDF(List<DadosExportacaoGrupo> dados)
    {
        // Implementação básica - seria ideal usar iTextSharp ou similar
        return await Task.Run(() =>
        {
            var html = new StringBuilder();
            html.AppendLine("<html><body>");
            html.AppendLine("<h1>Relatório de Grupos</h1>");
            html.AppendLine("<table border='1'>");
            html.AppendLine("<tr><th>ID</th><th>Nome</th><th>Descrição</th><th>Tipo</th><th>Ativo</th><th>Total Usuários</th></tr>");

            foreach (var item in dados)
            {
                html.AppendLine($"<tr><td>{item.Id}</td><td>{item.Nome}</td><td>{item.Descricao}</td><td>{item.Tipo}</td><td>{item.Ativo}</td><td>{item.TotalUsuarios}</td></tr>");
            }

            html.AppendLine("</table>");
            html.AppendLine("</body></html>");

            // Por simplicidade, retornar HTML como bytes
            return System.Text.Encoding.UTF8.GetBytes(html.ToString());
        });
    }

    /// <summary>
    /// Obtém content type baseado no formato
    /// </summary>
    private string ObterContentType(string formato)
    {
        return formato.ToLower() switch
        {
            "csv" => "text/csv",
            "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "json" => "application/json",
            "pdf" => "application/pdf",
            _ => "application/octet-stream"
        };
    }

    /// <summary>
    /// Gera resumo dos filtros aplicados
    /// </summary>
    private List<string> GerarResumoFiltros(BuscaAvancadaGruposRequest request)
    {
        var resumo = new List<string>();

        if (!string.IsNullOrEmpty(request.TermoBusca))
            resumo.Add($"Busca: '{request.TermoBusca}'");

        if (request.Tipos?.Any() == true)
            resumo.Add($"Tipos: {string.Join(", ", request.Tipos)}");

        if (request.Ativo.HasValue)
            resumo.Add($"Status: {(request.Ativo.Value ? "Ativo" : "Inativo")}");

        if (request.DataCriacaoInicio.HasValue)
            resumo.Add($"Criado após: {request.DataCriacaoInicio.Value:dd/MM/yyyy}");

        if (request.DataCriacaoFim.HasValue)
            resumo.Add($"Criado antes: {request.DataCriacaoFim.Value:dd/MM/yyyy}");

        if (request.MinUsuarios.HasValue)
            resumo.Add($"Mínimo {request.MinUsuarios.Value} usuários");

        if (request.MaxUsuarios.HasValue)
            resumo.Add($"Máximo {request.MaxUsuarios.Value} usuários");

        return resumo;
    }

    #endregion

    #region Classes Auxiliares de Exportação

    /// <summary>
    /// Dados formatados para exportação
    /// </summary>
    private class DadosExportacaoGrupo
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string Ativo { get; set; } = string.Empty;
        public string DataCriacao { get; set; } = string.Empty;
        public string DataAtualizacao { get; set; } = string.Empty;
        public int TotalUsuarios { get; set; }
        public int UsuariosAtivos { get; set; }
        public int UsuariosInativos { get; set; }
        public object? Usuarios { get; set; }
    }

    #endregion

    #region Operações em Lote (Bonus)

    /// <summary>
    /// Executa operações em lote com múltiplos grupos
    /// </summary>
    /// <param name="request">Dados da operação em lote</param>
    /// <returns>Resultado da operação</returns>
    [HttpPost("operacao-lote")]
    [ProducesResponseType(typeof(RespostaOperacaoLoteGrupos), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespostaErro), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExecutarOperacaoLote([FromBody] OperacaoLoteGruposRequest request)
    {
        try
        {
            if (!TemPermissao("Grupos.OperacaoLote"))
            {
                return StatusCode(403, new RespostaErro 
                { 
                    Erro = "Acesso negado", 
                    Mensagem = "Acesso negado para operações em lote com grupos" 
                });
            }

            var usuarioLogadoId = ObterUsuarioLogadoId();
            var operacao = request.Operacao.ToLower();

            var resultado = operacao switch
            {
                "ativar" => await ExecutarAtivacaoLote(request.GruposIds, usuarioLogadoId),
                "desativar" => await ExecutarDesativacaoLote(request.GruposIds, usuarioLogadoId),
                "excluir" => await ExecutarExclusaoLote(request.GruposIds, usuarioLogadoId, request.ExclusaoPermanente),
                "alterar-tipo" => await ExecutarAlteracaoTipoLote(request.GruposIds, request.NovoTipo ?? "", usuarioLogadoId),
                _ => throw new ArgumentException($"Operação '{request.Operacao}' não é válida")
            };

            await RegistrarAuditoria($"OPERACAO_LOTE_{operacao.ToUpper()}", "Grupos", 
                string.Join(",", request.GruposIds), 
                $"Operação em lote '{operacao}' executada em {request.GruposIds.Count} grupos", 
                request, resultado);

            return Ok(resultado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao executar operação em lote com grupos");
            return StatusCode(500, new RespostaErro { Erro = "Erro interno", Mensagem = "Erro ao processar solicitação" });
        }
    }

    /// <summary>
    /// Executa ativação em lote
    /// </summary>
    private async Task<RespostaOperacaoLoteGrupos> ExecutarAtivacaoLote(List<int> gruposIds, int usuarioLogadoId)
    {
        var grupos = await _context.Grupos
            .Where(g => gruposIds.Contains(g.Id) && !g.Ativo)
            .ToListAsync();

        foreach (var grupo in grupos)
        {
            grupo.Ativo = true;
            grupo.DataAtualizacao = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return new RespostaOperacaoLoteGrupos
        {
            Sucesso = true,
            Mensagem = $"{grupos.Count} grupo(s) ativado(s) com sucesso",
            TotalProcessados = grupos.Count,
            TotalSucesso = grupos.Count,
            TotalErros = 0,
            Detalhes = grupos.Select(g => $"Grupo '{g.Nome}' ativado").ToList()
        };
    }

    /// <summary>
    /// Executa desativação em lote
    /// </summary>
    private async Task<RespostaOperacaoLoteGrupos> ExecutarDesativacaoLote(List<int> gruposIds, int usuarioLogadoId)
    {
        var grupos = await _context.Grupos
            .Where(g => gruposIds.Contains(g.Id) && g.Ativo)
            .ToListAsync();

        foreach (var grupo in grupos)
        {
            grupo.Ativo = false;
            grupo.DataAtualizacao = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return new RespostaOperacaoLoteGrupos
        {
            Sucesso = true,
            Mensagem = $"{grupos.Count} grupo(s) desativado(s) com sucesso",
            TotalProcessados = grupos.Count,
            TotalSucesso = grupos.Count,
            TotalErros = 0,
            Detalhes = grupos.Select(g => $"Grupo '{g.Nome}' desativado").ToList()
        };
    }

    /// <summary>
    /// Executa exclusão em lote
    /// </summary>
    private async Task<RespostaOperacaoLoteGrupos> ExecutarExclusaoLote(List<int> gruposIds, int usuarioLogadoId, bool permanente)
    {
        var grupos = await _context.Grupos
            .Include(g => g.UsuarioGrupos)
            .Where(g => gruposIds.Contains(g.Id))
            .ToListAsync();

        var sucessos = 0;
        var erros = 0;
        var detalhes = new List<string>();

        foreach (var grupo in grupos)
        {
            try
            {
                if (permanente)
                {
                    if (grupo.UsuarioGrupos.Any(ug => ug.Ativo))
                    {
                        detalhes.Add($"Grupo '{grupo.Nome}' não pode ser excluído permanentemente (possui usuários)");
                        erros++;
                        continue;
                    }

                    _context.Grupos.Remove(grupo);
                    detalhes.Add($"Grupo '{grupo.Nome}' excluído permanentemente");
                }
                else
                {
                    grupo.Ativo = false;
                    grupo.DataAtualizacao = DateTime.UtcNow;
                    detalhes.Add($"Grupo '{grupo.Nome}' desativado");
                }

                sucessos++;
            }
            catch
            {
                detalhes.Add($"Erro ao processar grupo '{grupo.Nome}'");
                erros++;
            }
        }

        await _context.SaveChangesAsync();

        return new RespostaOperacaoLoteGrupos
        {
            Sucesso = erros == 0,
            Mensagem = $"Operação concluída: {sucessos} sucesso(s), {erros} erro(s)",
            TotalProcessados = grupos.Count,
            TotalSucesso = sucessos,
            TotalErros = erros,
            Detalhes = detalhes
        };
    }

    /// <summary>
    /// Executa alteração de tipo em lote
    /// </summary>
    private async Task<RespostaOperacaoLoteGrupos> ExecutarAlteracaoTipoLote(List<int> gruposIds, string novoTipo, int usuarioLogadoId)
    {
        var grupos = await _context.Grupos
            .Where(g => gruposIds.Contains(g.Id))
            .ToListAsync();

        foreach (var grupo in grupos)
        {
            grupo.Tipo = novoTipo;
            grupo.DataAtualizacao = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return new RespostaOperacaoLoteGrupos
        {
            Sucesso = true,
            Mensagem = $"{grupos.Count} grupo(s) alterado(s) para tipo '{novoTipo}'",
            TotalProcessados = grupos.Count,
            TotalSucesso = grupos.Count,
            TotalErros = 0,
            Detalhes = grupos.Select(g => $"Grupo '{g.Nome}' alterado para tipo '{novoTipo}'").ToList()
        };
    }

    #endregion
}