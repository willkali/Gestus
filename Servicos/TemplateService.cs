using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Gestus.Dados;
using Gestus.Modelos;
using Gestus.DTOs.Sistema;

namespace Gestus.Services;

public class TemplateService : ITemplateService
{
    private readonly GestusDbContexto _context;
    private readonly ILogger<TemplateService> _logger;
    private readonly IChaveVersaoService _chaveVersaoService;

    // Variáveis padrão do sistema por tipo
    private readonly Dictionary<string, List<Gestus.DTOs.Sistema.VariavelTemplate>> _variaveisPadrao = new()
    {
        {
            "RecuperarSenha", new List<Gestus.DTOs.Sistema.VariavelTemplate>
            {
                new() { Nome = "Nome do Usuário", Chave = "{NomeUsuario}", Descricao = "Nome completo do usuário", ExemploValor = "João Silva", Obrigatoria = true },
                new() { Nome = "Email do Usuário", Chave = "{EmailUsuario}", Descricao = "Email do usuário", ExemploValor = "joao@empresa.com", Obrigatoria = true },
                new() { Nome = "Link de Recuperação", Chave = "{LinkRecuperacao}", Descricao = "Link para redefinir senha", ExemploValor = "https://app.com/reset?token=abc123", Obrigatoria = true },
                new() { Nome = "Token", Chave = "{Token}", Descricao = "Token de recuperação", ExemploValor = "abc123def456", Obrigatoria = false },
                new() { Nome = "Data de Expiração", Chave = "{DataExpiracao}", Descricao = "Data/hora de expiração do token", ExemploValor = "25/12/2024 15:30", Obrigatoria = true },
                new() { Nome = "Nome do Sistema", Chave = "{NomeSistema}", Descricao = "Nome do sistema", ExemploValor = "Gestus", Obrigatoria = true }
            }
        },
        {
            "BoasVindas", new List<Gestus.DTOs.Sistema.VariavelTemplate>
            {
                new() { Nome = "Nome do Usuário", Chave = "{NomeUsuario}", Descricao = "Nome completo do usuário", ExemploValor = "João Silva", Obrigatoria = true },
                new() { Nome = "Email do Usuário", Chave = "{EmailUsuario}", Descricao = "Email do usuário", ExemploValor = "joao@empresa.com", Obrigatoria = true },
                new() { Nome = "Link de Login", Chave = "{LinkLogin}", Descricao = "Link para fazer login", ExemploValor = "https://app.com/login", Obrigatoria = true },
                new() { Nome = "Nome do Sistema", Chave = "{NomeSistema}", Descricao = "Nome do sistema", ExemploValor = "Gestus", Obrigatoria = true }
            }
        },
        {
            "ConfirmarEmail", new List<Gestus.DTOs.Sistema.VariavelTemplate>
            {
                new() { Nome = "Nome do Usuário", Chave = "{NomeUsuario}", Descricao = "Nome completo do usuário", ExemploValor = "João Silva", Obrigatoria = true },
                new() { Nome = "Email do Usuário", Chave = "{EmailUsuario}", Descricao = "Email do usuário", ExemploValor = "joao@empresa.com", Obrigatoria = true },
                new() { Nome = "Link de Confirmação", Chave = "{LinkConfirmacao}", Descricao = "Link para confirmar email", ExemploValor = "https://app.com/confirm?token=abc123", Obrigatoria = true },
                new() { Nome = "Token", Chave = "{Token}", Descricao = "Token de confirmação", ExemploValor = "abc123def456", Obrigatoria = false },
                new() { Nome = "Nome do Sistema", Chave = "{NomeSistema}", Descricao = "Nome do sistema", ExemploValor = "Gestus", Obrigatoria = true }
            }
        }
    };

    public TemplateService(
        GestusDbContexto context,
        ILogger<TemplateService> logger,
        IChaveVersaoService chaveVersaoService)
    {
        _context = context;
        _logger = logger;
        _chaveVersaoService = chaveVersaoService;
    }

    public async Task<ValidacaoTemplateResponse> ValidarTemplateAsync(ValidarTemplateRequest request)
    {
        try
        {
            var resposta = new ValidacaoTemplateResponse();
            
            if (!_variaveisPadrao.ContainsKey(request.Tipo))
            {
                resposta.Erros.Add($"Tipo de template '{request.Tipo}' não é suportado");
                return resposta;
            }

            var variaveisObrigatorias = _variaveisPadrao[request.Tipo].Where(v => v.Obrigatoria).ToList();
            var variaveisOpcionais = _variaveisPadrao[request.Tipo].Where(v => !v.Obrigatoria).ToList();

            // Encontrar todas as variáveis no template
            var regex = new Regex(@"\{(\w+)\}", RegexOptions.IgnoreCase);
            var variaveisEncontradas = new HashSet<string>();
            
            foreach (Match match in regex.Matches(request.Assunto))
            {
                variaveisEncontradas.Add(match.Groups[1].Value);
            }
            
            foreach (Match match in regex.Matches(request.CorpoHtml))
            {
                variaveisEncontradas.Add(match.Groups[1].Value);
            }

            // Verificar variáveis obrigatórias
            foreach (var variavel in variaveisObrigatorias)
            {
                var nomeVariavel = variavel.Chave.Trim('{', '}');
                
                if (variaveisEncontradas.Contains(nomeVariavel))
                {
                    resposta.VariaveisEncontradas.Add(new VariavelEncontrada
                    {
                        Nome = variavel.Nome,
                        Chave = variavel.Chave,
                        Obrigatoria = true,
                        Descricao = variavel.Descricao
                    });
                }
                else
                {
                    resposta.VariaveisFaltantes.Add(new VariavelFaltante
                    {
                        Nome = variavel.Nome,
                        Chave = variavel.Chave,
                        Descricao = variavel.Descricao,
                        ExemploValor = variavel.ExemploValor
                    });
                }
            }

            // Verificar variáveis opcionais
            foreach (var variavel in variaveisOpcionais)
            {
                var nomeVariavel = variavel.Chave.Trim('{', '}');
                
                if (variaveisEncontradas.Contains(nomeVariavel))
                {
                    resposta.VariaveisEncontradas.Add(new VariavelEncontrada
                    {
                        Nome = variavel.Nome,
                        Chave = variavel.Chave,
                        Obrigatoria = false,
                        Descricao = variavel.Descricao
                    });
                }
            }

            // Verificar variáveis desconhecidas
            var variaveisConhecidas = _variaveisPadrao[request.Tipo].Select(v => v.Chave.Trim('{', '}')).ToHashSet();
            var variaveisDesconhecidas = variaveisEncontradas.Except(variaveisConhecidas).ToList();
            
            foreach (var varDesconhecida in variaveisDesconhecidas)
            {
                resposta.Avisos.Add($"Variável '{{{varDesconhecida}}}' não é reconhecida pelo sistema");
            }

            // Definir se é válido
            resposta.Valido = resposta.VariaveisFaltantes.Count == 0;

            if (resposta.VariaveisFaltantes.Any())
            {
                resposta.Erros.Add($"Template está faltando {resposta.VariaveisFaltantes.Count} variável(is) obrigatória(s)");
            }

            // Gerar preview se válido
            if (resposta.Valido)
            {
                resposta.PreviewHtml = await GerarPreviewAsync(request.Tipo, request.CorpoHtml);
            }

            return resposta;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao validar template");
            return new ValidacaoTemplateResponse
            {
                Valido = false,
                Erros = { "Erro interno ao validar template" }
            };
        }
    }

    public async Task<List<TipoTemplateResponse>> ObterTiposTemplateAsync()
    {
        await Task.CompletedTask; // Para remover warning async
        
        return _variaveisPadrao.Select(kvp => new TipoTemplateResponse
        {
            Tipo = kvp.Key,
            Nome = kvp.Key switch
            {
                "RecuperarSenha" => "Recuperação de Senha",
                "BoasVindas" => "Boas-vindas",
                "ConfirmarEmail" => "Confirmação de Email",
                _ => kvp.Key
            },
            Descricao = kvp.Key switch
            {
                "RecuperarSenha" => "Template para emails de recuperação de senha",
                "BoasVindas" => "Template para emails de boas-vindas a novos usuários",
                "ConfirmarEmail" => "Template para emails de confirmação de endereço de email",
                _ => $"Template para {kvp.Key}"
            },
            VariaveisObrigatorias = kvp.Value.Where(v => v.Obrigatoria).ToList(),
            VariaveisOpcionais = kvp.Value.Where(v => !v.Obrigatoria).ToList()
        }).ToList();
    }

    public async Task<string> GerarPreviewAsync(string tipo, string template, Dictionary<string, string>? valores = null)
    {
        try
        {
            await Task.CompletedTask; // Para remover warning async
            
            if (!_variaveisPadrao.ContainsKey(tipo))
                return template;

            var valoresExemplo = valores ?? new Dictionary<string, string>();
            
            // Usar valores de exemplo se não fornecidos
            foreach (var variavel in _variaveisPadrao[tipo])
            {
                var nomeVariavel = variavel.Chave.Trim('{', '}');
                if (!valoresExemplo.ContainsKey(nomeVariavel))
                {
                    valoresExemplo[nomeVariavel] = variavel.ExemploValor ?? $"[{variavel.Nome}]";
                }
            }

            var resultado = template;
            foreach (var kvp in valoresExemplo)
            {
                resultado = resultado.Replace($"{{{kvp.Key}}}", kvp.Value);
            }

            return resultado;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao gerar preview do template");
            return template;
        }
    }

    public async Task<TemplatePersonalizadoResponse> CriarTemplateAsync(CriarTemplateRequest request, int usuarioId)
    {
        try
        {
            // Validar se o tipo é suportado
            if (!_variaveisPadrao.ContainsKey(request.Tipo))
            {
                throw new InvalidOperationException($"Tipo de template '{request.Tipo}' não é suportado");
            }

            // Validar o template antes de criar
            var validacao = await ValidarTemplateAsync(new ValidarTemplateRequest
            {
                Tipo = request.Tipo,
                Assunto = request.Assunto,
                CorpoHtml = request.CorpoHtml,
                CorpoTexto = request.CorpoTexto
            });

            if (!validacao.Valido)
            {
                throw new InvalidOperationException($"Template inválido: {string.Join(", ", validacao.Erros)}");
            }

            // Verificar se já existe template com o mesmo nome
            var existeNome = await _context.Set<TemplateEmailPersonalizado>()
                .AnyAsync(t => t.Nome.ToLower() == request.Nome.ToLower() && t.Ativo);

            if (existeNome)
            {
                throw new InvalidOperationException($"Já existe um template ativo com o nome '{request.Nome}'");
            }

            // Criar novo template
            var novoTemplate = new TemplateEmailPersonalizado
            {
                Nome = request.Nome.Trim(),
                Tipo = request.Tipo,
                Assunto = request.Assunto.Trim(),
                CorpoHtml = request.CorpoHtml,
                CorpoTexto = request.CorpoTexto?.Trim(),
                Descricao = request.Descricao?.Trim(),
                CriadoPorId = usuarioId,
                VariaveisObrigatorias = JsonSerializer.Serialize(
                    validacao.VariaveisEncontradas.Where(v => v.Obrigatoria).Select(v => v.Chave).ToList()
                ),
                VariaveisOpcionais = JsonSerializer.Serialize(
                    validacao.VariaveisEncontradas.Where(v => !v.Obrigatoria).Select(v => v.Chave).ToList()
                )
            };

            _context.Set<TemplateEmailPersonalizado>().Add(novoTemplate);
            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ Template personalizado criado - ID: {Id}, Nome: {Nome}, Tipo: {Tipo}", 
                novoTemplate.Id, novoTemplate.Nome, novoTemplate.Tipo);

            // Retornar resposta
            return await MapearParaResponse(novoTemplate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao criar template personalizado - Nome: {Nome}, Tipo: {Tipo}", 
                request.Nome, request.Tipo);
            throw;
        }
    }

    public async Task<TemplatePersonalizadoResponse?> AtualizarTemplateAsync(int templateId, CriarTemplateRequest request, int usuarioId)
    {
        try
        {
            var template = await _context.Set<TemplateEmailPersonalizado>()
                .Include(t => t.CriadoPor)
                .Include(t => t.AtualizadoPor)
                .FirstOrDefaultAsync(t => t.Id == templateId && t.Ativo);

            if (template == null)
            {
                _logger.LogWarning("⚠️ Template não encontrado - ID: {TemplateId}", templateId);
                return null;
            }

            // Validar se o tipo é suportado
            if (!_variaveisPadrao.ContainsKey(request.Tipo))
            {
                throw new InvalidOperationException($"Tipo de template '{request.Tipo}' não é suportado");
            }

            // Validar o template antes de atualizar
            var validacao = await ValidarTemplateAsync(new ValidarTemplateRequest
            {
                Tipo = request.Tipo,
                Assunto = request.Assunto,
                CorpoHtml = request.CorpoHtml,
                CorpoTexto = request.CorpoTexto
            });

            if (!validacao.Valido)
            {
                throw new InvalidOperationException($"Template inválido: {string.Join(", ", validacao.Erros)}");
            }

            // Verificar se já existe outro template com o mesmo nome
            var existeOutroComNome = await _context.Set<TemplateEmailPersonalizado>()
                .AnyAsync(t => t.Id != templateId && 
                              t.Nome.ToLower() == request.Nome.ToLower() && 
                              t.Ativo);

            if (existeOutroComNome)
            {
                throw new InvalidOperationException($"Já existe outro template ativo com o nome '{request.Nome}'");
            }

            // Atualizar template
            template.Nome = request.Nome.Trim();
            template.Tipo = request.Tipo;
            template.Assunto = request.Assunto.Trim();
            template.CorpoHtml = request.CorpoHtml;
            template.CorpoTexto = request.CorpoTexto?.Trim();
            template.Descricao = request.Descricao?.Trim();
            template.DataAtualizacao = DateTime.UtcNow;
            template.AtualizadoPorId = usuarioId;
            template.VariaveisObrigatorias = JsonSerializer.Serialize(
                validacao.VariaveisEncontradas.Where(v => v.Obrigatoria).Select(v => v.Chave).ToList()
            );
            template.VariaveisOpcionais = JsonSerializer.Serialize(
                validacao.VariaveisEncontradas.Where(v => !v.Obrigatoria).Select(v => v.Chave).ToList()
            );

            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ Template personalizado atualizado - ID: {Id}, Nome: {Nome}", 
                template.Id, template.Nome);

            return await MapearParaResponse(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao atualizar template - ID: {TemplateId}", templateId);
            throw;
        }
    }

    public async Task<bool> ExcluirTemplateAsync(int templateId, int usuarioId)
    {
        try
        {
            var template = await _context.Set<TemplateEmailPersonalizado>()
                .FirstOrDefaultAsync(t => t.Id == templateId && t.Ativo);

            if (template == null)
            {
                _logger.LogWarning("⚠️ Template não encontrado para exclusão - ID: {TemplateId}", templateId);
                return false;
            }

            // Exclusão lógica
            template.Ativo = false;
            template.DataAtualizacao = DateTime.UtcNow;
            template.AtualizadoPorId = usuarioId;

            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ Template personalizado excluído (soft delete) - ID: {Id}, Nome: {Nome}", 
                template.Id, template.Nome);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao excluir template - ID: {TemplateId}", templateId);
            return false;
        }
    }

    public async Task<List<TemplatePersonalizadoResponse>> ListarTemplatesAsync(string? tipo = null, bool? ativo = null)
    {
        try
        {
            var query = _context.Set<TemplateEmailPersonalizado>()
                .Include(t => t.CriadoPor)
                .Include(t => t.AtualizadoPor)
                .AsQueryable();

            // Filtros opcionais
            if (!string.IsNullOrEmpty(tipo))
            {
                query = query.Where(t => t.Tipo.ToLower() == tipo.ToLower());
            }

            if (ativo.HasValue)
            {
                query = query.Where(t => t.Ativo == ativo.Value);
            }

            var templates = await query
                .OrderByDescending(t => t.DataCriacao)
                .ToListAsync();

            var resultado = new List<TemplatePersonalizadoResponse>();
            
            foreach (var template in templates)
            {
                resultado.Add(await MapearParaResponse(template));
            }

            return resultado;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao listar templates - Tipo: {Tipo}, Ativo: {Ativo}", tipo, ativo);
            return new List<TemplatePersonalizadoResponse>();
        }
    }

    public async Task InicializarTemplatesPadraoAsync()
    {
        try
        {
            _logger.LogInformation("🌱 Inicializando templates padrão do sistema...");

            // Verificar se já existe configuração de email
            var configuracaoEmail = await _context.Set<ConfiguracaoEmail>()
                .FirstOrDefaultAsync(c => c.Ativo);

            if (configuracaoEmail == null)
            {
                _logger.LogInformation("⚠️ Nenhuma configuração de email encontrada. Criando configuração padrão...");
                
                // Criar configuração padrão (temporária)
                configuracaoEmail = new ConfiguracaoEmail
                {
                    ServidorSmtp = "smtp.gmail.com",
                    Porta = 587,
                    EmailRemetente = "noreply@gestus.local",
                    NomeRemetente = "Sistema Gestus",
                    SenhaEncriptada = await _chaveVersaoService.EncriptarComVersaoAsync("senha_temporaria", "EmailPassword"),
                    UsarSsl = true,
                    UsarTls = true,
                    UsarAutenticacao = true,
                    Ativo = false // Desativado até o admin configurar
                };

                _context.Set<ConfiguracaoEmail>().Add(configuracaoEmail);
                await _context.SaveChangesAsync();
            }

            // Criar templates padrão se não existirem
            await CriarTemplatePadraoSeNaoExistir(configuracaoEmail.Id, "RecuperarSenha");
            await CriarTemplatePadraoSeNaoExistir(configuracaoEmail.Id, "BoasVindas");
            await CriarTemplatePadraoSeNaoExistir(configuracaoEmail.Id, "ConfirmarEmail");

            _logger.LogInformation("✅ Templates padrão inicializados com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao inicializar templates padrão");
            throw;
        }
    }

    private async Task CriarTemplatePadraoSeNaoExistir(int configuracaoEmailId, string tipo)
    {
        var existe = await _context.Set<TemplateEmail>()
            .AnyAsync(t => t.Tipo == tipo && t.ConfiguracaoEmailId == configuracaoEmailId);

        if (!existe)
        {
            var template = new TemplateEmail
            {
                ConfiguracaoEmailId = configuracaoEmailId,
                Tipo = tipo,
                Assunto = ObterAssuntoPadrao(tipo),
                CorpoHtml = ObterCorpoHtmlPadrao(tipo),
                CorpoTexto = ObterCorpoTextoPadrao(tipo),
                Ativo = true, // ✅ ADICIONAR ESTA LINHA
                DataCriacao = DateTime.UtcNow // ✅ ADICIONAR ESTA LINHA TAMBÉM
            };

            _context.Set<TemplateEmail>().Add(template);
            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ Template padrão criado - Tipo: {Tipo}", tipo);
        }
        else
        {
            _logger.LogInformation("⚠️ Template padrão já existe - Tipo: {Tipo}", tipo);
        }
    }

    private async Task<TemplatePersonalizadoResponse> MapearParaResponse(TemplateEmailPersonalizado template)
    {
        // Validar o template novamente para incluir informações de validação
        var validacao = await ValidarTemplateAsync(new ValidarTemplateRequest
        {
            Tipo = template.Tipo,
            Assunto = template.Assunto,
            CorpoHtml = template.CorpoHtml,
            CorpoTexto = template.CorpoTexto
        });

        return new TemplatePersonalizadoResponse
        {
            Id = template.Id,
            Nome = template.Nome,
            Tipo = template.Tipo,
            Assunto = template.Assunto,
            CorpoHtml = template.CorpoHtml,
            CorpoTexto = template.CorpoTexto,
            Descricao = template.Descricao,
            Ativo = template.Ativo,
            IsTemplate = template.IsTemplate,
            CriadoPor = template.CriadoPor?.NomeCompleto ?? "Sistema",
            DataCriacao = template.DataCriacao,
            DataAtualizacao = template.DataAtualizacao,
            AtualizadoPor = template.AtualizadoPor?.NomeCompleto,
            
            // Informações de validação
            VariaveisObrigatoriasEncontradas = validacao.VariaveisEncontradas
                .Where(v => v.Obrigatoria)
                .Select(v => v.Chave)
                .ToList(),
            VariaveisObrigatoriasFaltantes = validacao.VariaveisFaltantes
                .Select(v => v.Chave)
                .ToList(),
            VariaveisOpcionaisEncontradas = validacao.VariaveisEncontradas
                .Where(v => !v.Obrigatoria)
                .Select(v => v.Chave)
                .ToList(),
            TemplateValido = validacao.Valido,
            MensagensValidacao = validacao.Erros.Concat(validacao.Avisos).ToList()
        };
    }

    private string ObterAssuntoPadrao(string tipo)
{
    return tipo switch
    {
        "BoasVindas" => "Bem-vindo(a) ao {NomeSistema}!",
        "RecuperarSenha" => "Recuperação de Senha - {NomeSistema}",
        "ConfirmarEmail" => "Confirme seu email - {NomeSistema}",
        _ => $"Notificação - {tipo}"
    };
}

    private string ObterCorpoHtmlPadrao(string tipo)
    {
        return tipo switch
        {
            "RecuperarSenha" => @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <title>Recuperação de Senha</title>
</head>
<body style='margin: 0; padding: 20px; font-family: Arial, sans-serif; background-color: #f5f5f5;'>
    <div style='max-width: 600px; margin: 0 auto; background-color: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 10px rgba(0,0,0,0.1);'>
        <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center;'>
            <h1 style='color: white; margin: 0; font-size: 28px; font-weight: 300;'>🔐 Recuperação de Senha</h1>
            <p style='color: rgba(255,255,255,0.9); margin: 10px 0 0 0; font-size: 16px;'>{NomeSistema}</p>
        </div>
        <div style='padding: 40px 30px;'>
            <h2 style='color: #333; margin: 0 0 20px 0; font-size: 24px; font-weight: 400;'>Olá, {NomeUsuario}!</h2>
            <p style='color: #666; line-height: 1.6; margin: 0 0 20px 0; font-size: 16px;'>
                Recebemos uma solicitação para redefinir a senha da sua conta em <strong>{EmailUsuario}</strong>.
            </p>
            <div style='text-align: center; margin: 30px 0;'>
                <a href='{LinkRecuperacao}' style='display: inline-block; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; font-weight: 500; font-size: 16px;'>
                    🔑 Redefinir Senha
                </a>
            </div>
            <div style='background: #f8f9ff; border-left: 4px solid #667eea; padding: 20px; margin: 30px 0; border-radius: 0 5px 5px 0;'>
                <p style='margin: 0; color: #555; font-size: 14px; line-height: 1.5;'>
                    <strong>⏰ Importante:</strong> Este link expira em <strong>{DataExpiracao}</strong> por motivos de segurança.
                </p>
            </div>
        </div>
        <div style='background: #f8f9fa; padding: 20px 30px; border-top: 1px solid #e9ecef; text-align: center;'>
            <p style='margin: 0; color: #6c757d; font-size: 12px; line-height: 1.5;'>
                Este é um email automático do <strong>{NomeSistema}</strong>.<br>
                Por favor, não responda a este email.
            </p>
        </div>
    </div>
</body>
</html>",

            "BoasVindas" => @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <title>Bem-vindo!</title>
</head>
<body style='margin: 0; padding: 20px; font-family: Arial, sans-serif; background-color: #f5f5f5;'>
    <div style='max-width: 600px; margin: 0 auto; background-color: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 10px rgba(0,0,0,0.1);'>
        <div style='background: linear-gradient(135deg, #28a745 0%, #20c997 100%); padding: 30px; text-align: center;'>
            <h1 style='color: white; margin: 0; font-size: 28px; font-weight: 300;'>🎉 Bem-vindo!</h1>
            <p style='color: rgba(255,255,255,0.9); margin: 10px 0 0 0; font-size: 16px;'>{NomeSistema}</p>
        </div>
        <div style='padding: 40px 30px;'>
            <h2 style='color: #333; margin: 0 0 20px 0; font-size: 24px; font-weight: 400;'>Olá, {NomeUsuario}!</h2>
            <p style='color: #666; line-height: 1.6; margin: 0 0 20px 0; font-size: 16px;'>
                Sua conta foi criada com sucesso no <strong>{NomeSistema}</strong>! 🎊
            </p>
            <div style='text-align: center; margin: 30px 0;'>
                <a href='{LinkLogin}' style='display: inline-block; background: linear-gradient(135deg, #28a745 0%, #20c997 100%); color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; font-weight: 500; font-size: 16px;'>
                    🚀 Acessar Sistema
                </a>
            </div>
        </div>
        <div style='background: #f8f9fa; padding: 20px 30px; border-top: 1px solid #e9ecef; text-align: center;'>
            <p style='margin: 0; color: #6c757d; font-size: 12px; line-height: 1.5;'>
                Este é um email automático do <strong>{NomeSistema}</strong>.
            </p>
        </div>
    </div>
</body>
</html>",

            "ConfirmarEmail" => @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <title>Confirmar Email</title>
</head>
<body style='margin: 0; padding: 20px; font-family: Arial, sans-serif; background-color: #f5f5f5;'>
    <div style='max-width: 600px; margin: 0 auto; background-color: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 10px rgba(0,0,0,0.1);'>
        <div style='background: linear-gradient(135deg, #007bff 0%, #6610f2 100%); padding: 30px; text-align: center;'>
            <h1 style='color: white; margin: 0; font-size: 28px; font-weight: 300;'>📧 Confirmar Email</h1>
            <p style='color: rgba(255,255,255,0.9); margin: 10px 0 0 0; font-size: 16px;'>{NomeSistema}</p>
        </div>
        <div style='padding: 40px 30px;'>
            <h2 style='color: #333; margin: 0 0 20px 0; font-size: 24px; font-weight: 400;'>Olá, {NomeUsuario}!</h2>
            <p style='color: #666; line-height: 1.6; margin: 0 0 20px 0; font-size: 16px;'>
                Para finalizar o cadastro, confirme seu email: <strong>{EmailUsuario}</strong>
            </p>
            <div style='text-align: center; margin: 30px 0;'>
                <a href='{LinkConfirmacao}' style='display: inline-block; background: linear-gradient(135deg, #007bff 0%, #6610f2 100%); color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; font-weight: 500; font-size: 16px;'>
                    ✅ Confirmar Email
                </a>
            </div>
        </div>
        <div style='background: #f8f9fa; padding: 20px 30px; border-top: 1px solid #e9ecef; text-align: center;'>
            <p style='margin: 0; color: #6c757d; font-size: 12px; line-height: 1.5;'>
                Este é um email automático do <strong>{NomeSistema}</strong>.
            </p>
        </div>
    </div>
</body>
</html>",

            _ => $@"
<html>
<body>
    <h2>{tipo} - {{NomeSistema}}</h2>
    <p>Olá {{NomeUsuario}},</p>
    <p>Esta é uma mensagem do sistema {{NomeSistema}}.</p>
    <p>Template padrão para o tipo: {tipo}</p>
</body>
</html>"
        };
    }

    private string ObterCorpoTextoPadrao(string tipo)
    {
        return tipo switch
        {
            "RecuperarSenha" => @"
RECUPERAÇÃO DE SENHA - {NomeSistema}

Olá {NomeUsuario}!

Recebemos uma solicitação para redefinir a senha da sua conta ({EmailUsuario}).

Para criar uma nova senha, acesse o link abaixo:
{LinkRecuperacao}

IMPORTANTE: Este link expira em {DataExpiracao} por motivos de segurança.

Se você não solicitou esta alteração, pode ignorar este email.

---
Este é um email automático do {NomeSistema}.",

            "BoasVindas" => @"
BEM-VINDO AO {NomeSistema}!

Olá {NomeUsuario}!

Sua conta foi criada com sucesso! 

Email: {EmailUsuario}

Para acessar o sistema, clique no link:
{LinkLogin}

---
Este é um email automático do {NomeSistema}.",

            "ConfirmarEmail" => @"
CONFIRMAR EMAIL - {NomeSistema}

Olá {NomeUsuario}!

Para finalizar o cadastro, confirme seu email: {EmailUsuario}

Clique no link abaixo para confirmar:
{LinkConfirmacao}

Se você não criou uma conta no {NomeSistema}, ignore este email.

---
Este é um email automático do {NomeSistema}.",

            _ => $@"
{tipo.ToUpper()} - {{NomeSistema}}

Olá {{NomeUsuario}},

Esta é uma mensagem do sistema {{NomeSistema}}.
Template padrão para o tipo: {tipo}

Email: {{EmailUsuario}}"
        };
    }
}