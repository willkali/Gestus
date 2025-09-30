using Gestus.Modelos;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace Gestus.Dados;

public static class SeederInicial
{
    public static async Task ExecutarAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<GestusDbContexto>();
        var userManager = serviceProvider.GetRequiredService<UserManager<Usuario>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<Papel>>();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();

        // ✅ ADICIONAR: Gerenciador de aplicações e scopes OpenIddict
        var applicationManager = serviceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var scopeManager = serviceProvider.GetRequiredService<IOpenIddictScopeManager>();

        try
        {
            logger.LogInformation("🌱 Iniciando seeder de dados iniciais...");

            // Verificar conexão com banco
            var canConnect = await context.Database.CanConnectAsync();
            if (!canConnect)
            {
                logger.LogError("❌ Não foi possível conectar ao banco de dados");
                return;
            }

            logger.LogInformation("📊 Conexão com banco: OK");

            // ✅ ORDEM CORRETA DE EXECUÇÃO
            await CriarScopesOpenIddict(scopeManager, logger);
            await CriarAplicacoesOpenIddict(applicationManager, configuration, logger);
            await CriarPapeisBase(roleManager, logger);
            await CriarPermissoesBase(context, logger);
            await AssociarPermissoesAosPapeis(context, roleManager, logger);
            await CriarSuperAdmin(userManager, logger);
            await CriarGruposBase(context, logger);
            await CriarTemplatesEmail(context, logger);

            // ✅ TIPOS/STATUS DE APLICAÇÃO E APLICAÇÃO GESTUS
            await CriarTiposStatusAplicacaoBase(context, logger);
            await CriarAplicacaoGestus(context, configuration, logger);

            // ✅ CRIAR USUÁRIOS PADRÃO
            var superAdmin = await CriarSuperAdmin(userManager, logger);
            var adminUsuario = await CriarAdministrador(userManager, logger);

            // ✅ CONCEDER ACESSO À APLICAÇÃO GESTUS PARA SUPERADMIN E ADMIN
            await ConcederAcessoAplicacaoGestus(context, logger, superAdmin?.Id);
            await ConcederAcessoAplicacaoGestus(context, logger, adminUsuario?.Id);

            // Salvar mudanças finais
            logger.LogInformation("💾 Salvando mudanças no banco...");
            var mudancasSalvas = await context.SaveChangesAsync();
            logger.LogInformation($"💾 {mudancasSalvas} mudanças salvas no banco");

            logger.LogInformation("✅ Seeder executado com sucesso!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Erro durante a execução do seeder");
            throw;
        }
    }

    private static async Task CriarTemplatesEmail(GestusDbContexto context, ILogger logger)
    {
        logger.LogInformation("📧 Criando templates de email...");

        // Buscar configuração de email existente
        var configuracaoEmail = await context.Set<ConfiguracaoEmail>()
            .Where(c => c.Ativo)
            .FirstOrDefaultAsync();

        if (configuracaoEmail == null)
        {
            logger.LogWarning("⚠️ Nenhuma configuração de email encontrada. Pulando criação de templates.");
            return;
        }

        // ✅ CRIAR TODOS OS TEMPLATES DE UMA VEZ
        await CriarTemplateSeNaoExistir(context, configuracaoEmail.Id, "BoasVindas", logger);
        await CriarTemplateSeNaoExistir(context, configuracaoEmail.Id, "RecuperarSenha", logger);
        await CriarTemplateSeNaoExistir(context, configuracaoEmail.Id, "ConfirmarEmail", logger);
    }

    private static async Task CriarTemplateSeNaoExistir(GestusDbContexto context, int configuracaoEmailId, string tipo, ILogger logger)
    {
        var templateExists = await context.Set<TemplateEmail>()
            .AnyAsync(t => t.Tipo == tipo && t.ConfiguracaoEmailId == configuracaoEmailId);

        if (!templateExists)
        {
            var template = new TemplateEmail
            {
                ConfiguracaoEmailId = configuracaoEmailId,
                Tipo = tipo,
                Assunto = ObterAssuntoPadrao(tipo),
                CorpoHtml = ObterCorpoHtmlPadrao(tipo),
                CorpoTexto = ObterCorpoTextoPadrao(tipo),
                Ativo = true,
                DataCriacao = DateTime.UtcNow
            };

            context.Set<TemplateEmail>().Add(template);
            logger.LogInformation($"✅ Template '{tipo}' criado");
        }
        else
        {
            logger.LogInformation($"⚠️ Template '{tipo}' já existe");
        }
    }

    private static string ObterAssuntoPadrao(string tipo)
    {
        return tipo switch
        {
            "BoasVindas" => "Bem-vindo(a) ao {NomeSistema}!",
            "RecuperarSenha" => "Recuperação de Senha - {NomeSistema}",
            "ConfirmarEmail" => "Confirme seu email - {NomeSistema}",
            _ => $"Notificação - {tipo}"
        };
    }

    private static string ObterCorpoHtmlPadrao(string tipo)
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

    private static string ObterCorpoTextoPadrao(string tipo)
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

    private static async Task CriarAplicacoesOpenIddict(IOpenIddictApplicationManager applicationManager, IConfiguration configuration, ILogger logger)
    {
        logger.LogInformation("🔧 Criando aplicações OpenIddict...");

        // Carregar configurações de clients do OpenIddict
        var oidcSection = configuration.GetSection("OpenIddict");
        var apiClientId = oidcSection.GetValue<string>("Api:ClientId") ?? "gestus_api";
        var apiClientSecret = oidcSection.GetValue<string>("Api:ClientSecret") ?? "gestus_api_secret_2024";

        var spaClientId = oidcSection.GetValue<string>("Spa:ClientId") ?? "gestus_spa";
        var spaRedirectUris = oidcSection.GetSection("Spa:RedirectUris").Get<string[]>() ?? Array.Empty<string>();
        var spaPostLogoutUris = oidcSection.GetSection("Spa:PostLogoutRedirectUris").Get<string[]>() ?? Array.Empty<string>();

        // Aplicação para API (Resource Server)
        if (await applicationManager.FindByClientIdAsync(apiClientId) == null)
        {
            await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = apiClientId,
                ClientSecret = apiClientSecret,
                ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
                DisplayName = "Gestus API",
                ClientType = OpenIddictConstants.ClientTypes.Confidential,
                Permissions =
        {
            OpenIddictConstants.Permissions.Endpoints.Token,
            OpenIddictConstants.Permissions.Endpoints.Introspection,
            OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
            OpenIddictConstants.Permissions.GrantTypes.Password,
            OpenIddictConstants.Permissions.GrantTypes.RefreshToken, // ✅ Refresh token grant
            OpenIddictConstants.Permissions.Scopes.Email,
            OpenIddictConstants.Permissions.Scopes.Profile,
            OpenIddictConstants.Permissions.Scopes.Roles,
            // ✅ CORREÇÃO: Usar a constante correta para offline_access
            OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.OfflineAccess,
            "scp:openid"
        }
            });

            logger.LogInformation("✅ Aplicação '{ClientId}' criada no OpenIddict", apiClientId);
        }
        else
        {
            logger.LogInformation("⚠️ Aplicação '{ClientId}' já existe no OpenIddict", apiClientId);

            // ✅ VERIFICAR SE PRECISA ATUALIZAR PERMISSÕES
            var existingApp = await applicationManager.FindByClientIdAsync(apiClientId);
            if (existingApp != null)
            {
                var descriptor = new OpenIddictApplicationDescriptor();
                await applicationManager.PopulateAsync(descriptor, existingApp);

                // ✅ CORREÇÃO: Verificar com a constante correta
                var offlineAccessPermission = OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.OfflineAccess;
                if (!descriptor.Permissions.Contains(offlineAccessPermission))
                {
                    descriptor.Permissions.Add(offlineAccessPermission);
                    await applicationManager.UpdateAsync(existingApp, descriptor);
                    logger.LogInformation("✅ Permissão offline_access adicionada à aplicação '{ClientId}'", apiClientId);
                }
            }
        }

        // Aplicação para Frontend (SPA)
        if (await applicationManager.FindByClientIdAsync(spaClientId) == null)
        {
            var descriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = spaClientId,
                ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
                DisplayName = "Gestus SPA",
                ClientType = OpenIddictConstants.ClientTypes.Public,
                Permissions =
        {
            OpenIddictConstants.Permissions.Endpoints.Authorization,
            OpenIddictConstants.Permissions.Endpoints.Token,
            OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
            OpenIddictConstants.Permissions.GrantTypes.RefreshToken, // ✅ Refresh token para SPA
            OpenIddictConstants.Permissions.ResponseTypes.Code,
            OpenIddictConstants.Permissions.Scopes.Email,
            OpenIddictConstants.Permissions.Scopes.Profile,
            OpenIddictConstants.Permissions.Scopes.Roles,
            // ✅ CORREÇÃO: Usar a constante correta para offline_access
            OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.OfflineAccess,
            "scp:openid"
        },
                Requirements =
        {
            OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange
        }
            };

            // Adicionar URIs de configuração se houver
            if (spaRedirectUris.Length == 0 || spaPostLogoutUris.Length == 0)
            {
                logger.LogWarning("⚠️ URIs do SPA não configuradas (OpenIddict:Spa:RedirectUris / PostLogoutRedirectUris). Usando padrão http(s)://localhost:3000");
                descriptor.RedirectUris.Add(new Uri("http://localhost:3000/callback"));
                descriptor.PostLogoutRedirectUris.Add(new Uri("http://localhost:3000/"));
                descriptor.RedirectUris.Add(new Uri("https://localhost:3000/callback"));
                descriptor.PostLogoutRedirectUris.Add(new Uri("https://localhost:3000/"));
            }
            else
            {
                foreach (var uri in spaRedirectUris) descriptor.RedirectUris.Add(new Uri(uri));
                foreach (var uri in spaPostLogoutUris) descriptor.PostLogoutRedirectUris.Add(new Uri(uri));
            }

            await applicationManager.CreateAsync(descriptor);
            logger.LogInformation("✅ Aplicação '{ClientId}' criada no OpenIddict", spaClientId);
        }
        else
        {
            logger.LogInformation("⚠️ Aplicação '{ClientId}' já existe no OpenIddict", spaClientId);
        }
    }

    private static async Task CriarPapeisBase(RoleManager<Papel> roleManager, ILogger logger)
    {
        logger.LogInformation("📋 Criando papéis base...");

        var papeisBase = new[]
        {
            new { Nome = "SuperAdmin", Descricao = "Super Administrador - Controle total do sistema", Categoria = "Sistema", Nivel = 1000 },
            new { Nome = "Admin", Descricao = "Administrador - Gerenciamento operacional", Categoria = "Sistema", Nivel = 500 },
            new { Nome = "Usuario", Descricao = "Usuário - Acesso básico ao sistema", Categoria = "Sistema", Nivel = 100 },
            new { Nome = "GestorUsuarios", Descricao = "Gestor de Usuários - Pode gerenciar usuários", Categoria = "Funcional", Nivel = 300 },
            new { Nome = "GestorPermissoes", Descricao = "Gestor de Permissões - Pode gerenciar permissões", Categoria = "Funcional", Nivel = 400 },
            new { Nome = "Auditor", Descricao = "Auditor - Acesso apenas leitura para auditoria", Categoria = "Funcional", Nivel = 200 }
        };

        var papeisCountAntes = await roleManager.Roles.CountAsync();
        logger.LogInformation($"📋 Papéis existentes antes: {papeisCountAntes}");

        foreach (var papelInfo in papeisBase)
        {
            logger.LogInformation($"📋 Verificando papel: {papelInfo.Nome}");

            if (!await roleManager.RoleExistsAsync(papelInfo.Nome))
            {
                var novoPapel = new Papel
                {
                    Name = papelInfo.Nome,
                    NormalizedName = papelInfo.Nome.ToUpper(),
                    Descricao = papelInfo.Descricao,
                    Categoria = papelInfo.Categoria,
                    Nivel = papelInfo.Nivel,
                    Ativo = true
                };

                var resultado = await roleManager.CreateAsync(novoPapel);
                if (resultado.Succeeded)
                {
                    logger.LogInformation($"✅ Papel criado: {papelInfo.Nome}");
                }
                else
                {
                    logger.LogError($"❌ Erro ao criar papel {papelInfo.Nome}: {string.Join(", ", resultado.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                logger.LogInformation($"⚠️  Papel '{papelInfo.Nome}' já existe");
            }
        }

        var papeisCountDepois = await roleManager.Roles.CountAsync();
        logger.LogInformation($"📋 Papéis existentes depois: {papeisCountDepois}");
    }

    private static async Task<Usuario?> CriarSuperAdmin(UserManager<Usuario> userManager, ILogger logger)
    {
        logger.LogInformation("👑 Criando usuário Super Admin...");
        logger.LogInformation("👑 Verificando se usuário willian.cavalcante@skymsen.com já existe...");

        var emailSuper = "willian.cavalcante@skymsen.com";
        var superAdminExiste = await userManager.FindByEmailAsync(emailSuper);

        if (superAdminExiste == null)
        {
            var superAdmin = new Usuario
            {
                UserName = emailSuper,
                Email = emailSuper,
                EmailConfirmed = true,
                Nome = "Super",
                Sobrenome = "Admin",
                NomeCompleto = "Super Admin",
                Ativo = true,
                DataCriacao = DateTime.UtcNow
            };

            var resultado = await userManager.CreateAsync(superAdmin, "Reboot3!");

            if (resultado.Succeeded)
            {
                logger.LogInformation($"✅ Super Admin criado com ID: {superAdmin.Id}");

                var papelSuperAdminExiste = await userManager.IsInRoleAsync(superAdmin, "SuperAdmin");
                if (!papelSuperAdminExiste)
                {
                    var resultadoPapel = await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
                    if (resultadoPapel.Succeeded)
                    {
                        logger.LogInformation("✅ Papel SuperAdmin atribuído ao usuário");
                        return superAdmin;
                    }
                    else
                    {
                        logger.LogError($"❌ Erro ao atribuir papel SuperAdmin: {string.Join(", ", resultadoPapel.Errors.Select(e => e.Description))}");
                        return superAdmin;
                    }
                }
                return superAdmin;
            }
            else
            {
                logger.LogError($"❌ Erro ao criar Super Admin: {string.Join(", ", resultado.Errors.Select(e => e.Description))}");
                return null;
            }
        }
        else
        {
            logger.LogInformation($"⚠️  Super Admin já existe (ID: {superAdminExiste.Id})");

            var temPapel = await userManager.IsInRoleAsync(superAdminExiste, "SuperAdmin");
            if (!temPapel)
            {
                var resultadoPapel = await userManager.AddToRoleAsync(superAdminExiste, "SuperAdmin");
                if (resultadoPapel.Succeeded)
                {
                    logger.LogInformation("✅ Papel SuperAdmin atribuído ao usuário existente");
                }
                else
                {
                    logger.LogError($"❌ Erro ao atribuir papel SuperAdmin ao usuário existente: {string.Join(", ", resultadoPapel.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                logger.LogInformation($"🔍 Usuário tem papel SuperAdmin: {temPapel}");
            }

            return superAdminExiste;
        }
    }

    private static async Task CriarPermissoesBase(GestusDbContexto context, ILogger logger)
    {
        logger.LogInformation("🔐 Criando permissões base...");

        var permissoesBase = new[]
        {
            // ✅ PERMISSÕES EXISTENTES (manter)
            new { Nome = "Usuarios.Listar", Descricao = "Listar usuários", Recurso = "Usuarios", Acao = "Listar", Categoria = "Sistema" },
            new { Nome = "Usuarios.Visualizar", Descricao = "Visualizar detalhes de usuários", Recurso = "Usuarios", Acao = "Visualizar", Categoria = "Sistema" },
            new { Nome = "Usuarios.Criar", Descricao = "Criar novos usuários", Recurso = "Usuarios", Acao = "Criar", Categoria = "Sistema" },
            new { Nome = "Usuarios.Editar", Descricao = "Editar usuários existentes", Recurso = "Usuarios", Acao = "Editar", Categoria = "Sistema" },
            new { Nome = "Usuarios.Remover", Descricao = "Remover usuários (soft delete)", Recurso = "Usuarios", Acao = "Remover", Categoria = "Sistema" },
            new { Nome = "Usuarios.Reativar", Descricao = "Reativar usuários desativados", Recurso = "Usuarios", Acao = "Reativar", Categoria = "Sistema" },
            
            // ✅ NOVAS PERMISSÕES CRÍTICAS PARA SUPER ADMIN
            new { Nome = "Usuarios.ExcluirPermanente", Descricao = "Exclusão permanente de usuários (hard delete)", Recurso = "Usuarios", Acao = "ExcluirPermanente", Categoria = "Sistema" },
            new { Nome = "Usuarios.AlterarSenha", Descricao = "Alterar senha de outros usuários", Recurso = "Usuarios", Acao = "AlterarSenha", Categoria = "Sistema" },
            new { Nome = "Usuarios.GerenciarPapeis", Descricao = "Gerenciar papéis de usuários", Recurso = "Usuarios", Acao = "GerenciarPapeis", Categoria = "Sistema" },
            new { Nome = "Usuarios.BuscaAvancada", Descricao = "Busca avançada de usuários", Recurso = "Usuarios", Acao = "BuscaAvancada", Categoria = "Sistema" },
            new { Nome = "Usuarios.OperacoesLote", Descricao = "Operações em lote com usuários", Recurso = "Usuarios", Acao = "OperacoesLote", Categoria = "Sistema" },
            
            // ✅ PERMISSÕES DE EMAIL (ADICIONAR ESTA QUE ESTÁ FALTANDO)
            new { Nome = "Email.Listar", Descricao = "Listar configurações de email", Recurso = "Email", Acao = "Listar", Categoria = "Sistema" },
            new { Nome = "Email.Visualizar", Descricao = "Visualizar configurações de email", Recurso = "Email", Acao = "Visualizar", Categoria = "Sistema" },
            new { Nome = "Email.Criar", Descricao = "Criar configurações de email", Recurso = "Email", Acao = "Criar", Categoria = "Sistema" },
            new { Nome = "Email.Editar", Descricao = "Editar configurações de email", Recurso = "Email", Acao = "Editar", Categoria = "Sistema" },
            new { Nome = "Email.Remover", Descricao = "Remover configurações de email", Recurso = "Email", Acao = "Remover", Categoria = "Sistema" },
            new { Nome = "Email.Enviar", Descricao = "Enviar emails", Recurso = "Email", Acao = "Enviar", Categoria = "Sistema" },
            new { Nome = "Email.Configurar", Descricao = "Configurar templates de email", Recurso = "Email", Acao = "Configurar", Categoria = "Sistema" },
            
            // ✅ OUTRAS PERMISSÕES...
            new { Nome = "Papeis.Listar", Descricao = "Listar papéis", Recurso = "Papeis", Acao = "Listar", Categoria = "Sistema" },
            new { Nome = "Papeis.Visualizar", Descricao = "Visualizar detalhes de papéis", Recurso = "Papeis", Acao = "Visualizar", Categoria = "Sistema" },
            new { Nome = "Papeis.Criar", Descricao = "Criar novos papéis", Recurso = "Papeis", Acao = "Criar", Categoria = "Sistema" },
            new { Nome = "Papeis.Editar", Descricao = "Editar papéis existentes", Recurso = "Papeis", Acao = "Editar", Categoria = "Sistema" },
            new { Nome = "Papeis.Remover", Descricao = "Remover papéis", Recurso = "Papeis", Acao = "Remover", Categoria = "Sistema" },
            new { Nome = "Papeis.GerenciarPermissoes", Descricao = "Gerenciar permissões de papéis", Recurso = "Papeis", Acao = "GerenciarPermissoes", Categoria = "Sistema" },

            new { Nome = "Permissoes.Listar", Descricao = "Listar permissões", Recurso = "Permissoes", Acao = "Listar", Categoria = "Sistema" },
            new { Nome = "Permissoes.Visualizar", Descricao = "Visualizar detalhes de permissões", Recurso = "Permissoes", Acao = "Visualizar", Categoria = "Sistema" },
            new { Nome = "Permissoes.Criar", Descricao = "Criar novas permissões", Recurso = "Permissoes", Acao = "Criar", Categoria = "Sistema" },
            new { Nome = "Permissoes.Editar", Descricao = "Editar permissões existentes", Recurso = "Permissoes", Acao = "Editar", Categoria = "Sistema" },
            new { Nome = "Permissoes.Remover", Descricao = "Remover permissões", Recurso = "Permissoes", Acao = "Remover", Categoria = "Sistema" },

            new { Nome = "Grupos.Listar", Descricao = "Listar grupos", Recurso = "Grupos", Acao = "Listar", Categoria = "Sistema" },
            new { Nome = "Grupos.Visualizar", Descricao = "Visualizar detalhes de grupos", Recurso = "Grupos", Acao = "Visualizar", Categoria = "Sistema" },
            new { Nome = "Grupos.Criar", Descricao = "Criar novos grupos", Recurso = "Grupos", Acao = "Criar", Categoria = "Sistema" },
            new { Nome = "Grupos.Editar", Descricao = "Editar grupos existentes", Recurso = "Grupos", Acao = "Editar", Categoria = "Sistema" },
            new { Nome = "Grupos.Remover", Descricao = "Remover grupos", Recurso = "Grupos", Acao = "Remover", Categoria = "Sistema" },
            new { Nome = "Grupos.GerenciarMembros", Descricao = "Gerenciar membros de grupos", Recurso = "Grupos", Acao = "GerenciarMembros", Categoria = "Sistema" },

            new { Nome = "Auditoria.Listar", Descricao = "Listar registros de auditoria", Recurso = "Auditoria", Acao = "Listar", Categoria = "Sistema" },
            new { Nome = "Auditoria.Visualizar", Descricao = "Visualizar detalhes de auditoria", Recurso = "Auditoria", Acao = "Visualizar", Categoria = "Sistema" },
            new { Nome = "Auditoria.Exportar", Descricao = "Exportar dados de auditoria", Recurso = "Auditoria", Acao = "Exportar", Categoria = "Sistema" },

            new { Nome = "Sistema.Configurar", Descricao = "Configurar sistema", Recurso = "Sistema", Acao = "Configurar", Categoria = "Sistema" },
            new { Nome = "Sistema.Monitorar", Descricao = "Monitorar sistema e logs", Recurso = "Sistema", Acao = "Monitorar", Categoria = "Sistema" },
            new { Nome = "Sistema.Backup", Descricao = "Fazer backup do sistema", Recurso = "Sistema", Acao = "Backup", Categoria = "Sistema" },
            new { Nome = "Sistema.Restore", Descricao = "Restaurar backup do sistema", Recurso = "Sistema", Acao = "Restore", Categoria = "Sistema" }
        };

        // ✅ VERIFICAÇÃO CORRETA DE DUPLICAÇÃO (por RECURSO e AÇÃO)
        var permissoesExistentes = await context.Permissoes
            .Select(p => new { p.Recurso, p.Acao })
            .ToListAsync();

        var permissoesParaCriar = new List<Permissao>();

        foreach (var permissaoInfo in permissoesBase)
        {
            // ✅ VERIFICAR POR RECURSO E AÇÃO (não por nome)
            var permissaoExiste = permissoesExistentes.Any(p =>
                p.Recurso == permissaoInfo.Recurso && p.Acao == permissaoInfo.Acao);

            if (!permissaoExiste)
            {
                permissoesParaCriar.Add(new Permissao
                {
                    Nome = permissaoInfo.Nome,
                    Descricao = permissaoInfo.Descricao,
                    Recurso = permissaoInfo.Recurso,
                    Acao = permissaoInfo.Acao,
                    Categoria = permissaoInfo.Categoria,
                    Ativo = true,
                    DataCriacao = DateTime.UtcNow
                });
            }
        }

        if (permissoesParaCriar.Any())
        {
            try
            {
                context.Permissoes.AddRange(permissoesParaCriar);
                await context.SaveChangesAsync();
                logger.LogInformation($"✅ {permissoesParaCriar.Count} novas permissões criadas");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Erro ao criar permissões. Algumas podem já existir.");

                // ✅ SALVAR UMA POR UMA PARA IDENTIFICAR DUPLICATAS
                var sucessos = 0;
                foreach (var permissao in permissoesParaCriar)
                {
                    try
                    {
                        // ✅ VERIFICAR NOVAMENTE ANTES DE INSERIR
                        var jaExiste = await context.Permissoes
                            .AnyAsync(p => p.Recurso == permissao.Recurso && p.Acao == permissao.Acao);

                        if (!jaExiste)
                        {
                            context.Permissoes.Add(permissao);
                            await context.SaveChangesAsync();
                            sucessos++;
                            logger.LogInformation($"✅ Permissão {permissao.Nome} criada");
                        }
                        else
                        {
                            logger.LogInformation($"⚠️ Permissão {permissao.Nome} já existe");
                        }
                    }
                    catch (Exception permEx)
                    {
                        logger.LogWarning($"⚠️ Erro ao criar permissão {permissao.Nome}: {permEx.Message}");

                        // ✅ LIMPAR TRACKING PARA PRÓXIMA TENTATIVA
                        context.Entry(permissao).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    }
                }

                if (sucessos > 0)
                {
                    logger.LogInformation($"✅ {sucessos} permissões criadas com sucesso");
                }
            }
        }
        else
        {
            logger.LogInformation("⚠️ Todas as permissões já existem");
        }

        var totalPermissoes = await context.Permissoes.CountAsync();
        logger.LogInformation($"📋 Total de permissões no sistema: {totalPermissoes}");
    }

    private static async Task AssociarPermissoesAosPapeis(GestusDbContexto context, RoleManager<Papel> roleManager, ILogger logger)
    {
        logger.LogInformation("🔗 Associando permissões aos papéis...");

        var todasPermissoes = await context.Permissoes.Where(p => p.Ativo).ToListAsync();

        // ✅ SuperAdmin atua como usuário root e não precisa de permissões explícitas
        var superAdminPapel = await roleManager.FindByNameAsync("SuperAdmin");
        if (superAdminPapel != null)
        {
            logger.LogInformation("👑 SuperAdmin não requer associação explícita de permissões (bypass total)");
        }
        else
        {
            logger.LogError("❌ Papel SuperAdmin não encontrado!");
        }

        // ✅ ADMIN - PERMISSÕES OPERACIONAIS (SEM SISTEMA CRÍTICO)
        var adminPapel = await roleManager.FindByNameAsync("Admin");
        if (adminPapel != null)
        {
            // ✅ Administrador: TODAS as permissões, exceto gerenciamento de papéis/administradores/super admin e exclusão permanente de usuários
            var permissoesAdmin = todasPermissoes
                .Where(p =>
                    // Excluir capacidades de gerenciar papéis
                    !(p.Recurso == "Usuarios" && p.Acao == "GerenciarPapeis") &&
                    !(p.Recurso == "Papeis" && new[] { "Criar", "Editar", "Remover", "GerenciarPermissoes" }.Contains(p.Acao)) &&
                    // Por segurança, não permitir exclusão permanente de usuários
                    !(p.Recurso == "Usuarios" && p.Acao == "ExcluirPermanente")
                )
                .ToList();

            await AssociarPermissoesAoPapel(context, adminPapel.Id, permissoesAdmin, logger, "Admin");
            logger.LogInformation($"✅ Admin agora tem {permissoesAdmin.Count} permissões (com ressalvas)");
        }

        // ✅ USUÁRIO - PERMISSÕES BÁSICAS
        var usuarioPapel = await roleManager.FindByNameAsync("Usuario");
        if (usuarioPapel != null)
        {
            var permissoesUsuario = todasPermissoes.Where(p =>
                // Apenas visualizar próprio perfil (será controlado no controller)
                (p.Recurso == "Usuarios" && p.Acao == "Visualizar")
            ).ToList();

            await AssociarPermissoesAoPapel(context, usuarioPapel.Id, permissoesUsuario, logger, "Usuario");
            logger.LogInformation($"✅ Usuario agora tem {permissoesUsuario.Count} permissões");
        }

        // ✅ GESTOR DE USUÁRIOS - PERMISSÕES DE USUÁRIOS
        var gestorUsuariosPapel = await roleManager.FindByNameAsync("GestorUsuarios");
        if (gestorUsuariosPapel != null)
        {
            var permissoesGestorUsuarios = todasPermissoes.Where(p =>
                // Usuários (sem exclusão permanente e sem gerenciar papéis de admin)
                (p.Recurso == "Usuarios" && !new[] { "ExcluirPermanente", "GerenciarPapeis" }.Contains(p.Acao)) ||
                // Grupos (visualizar)
                (p.Recurso == "Grupos" && new[] { "Listar", "Visualizar" }.Contains(p.Acao))
            ).ToList();

            await AssociarPermissoesAoPapel(context, gestorUsuariosPapel.Id, permissoesGestorUsuarios, logger, "GestorUsuarios");
            logger.LogInformation($"✅ GestorUsuarios agora tem {permissoesGestorUsuarios.Count} permissões");
        }
        else
        {
            logger.LogWarning("⚠️ Papel GestorUsuarios não encontrado");
        }

        logger.LogInformation("🔗 Associação de permissões concluída!");
    }

    private static async Task AssociarPermissoesAoPapel(GestusDbContexto context, int papelId, List<Permissao> permissoes, ILogger logger, string nomePapel)
    {
        var permissoesExistentes = await context.PapelPermissoes
            .Where(pp => pp.PapelId == papelId)
            .Select(pp => pp.PermissaoId)
            .ToListAsync();

        var novasAssociacoes = new List<PapelPermissao>();

        foreach (var permissao in permissoes)
        {
            if (!permissoesExistentes.Contains(permissao.Id))
            {
                novasAssociacoes.Add(new PapelPermissao
                {
                    PapelId = papelId,
                    PermissaoId = permissao.Id,
                    DataAtribuicao = DateTime.UtcNow,
                    Ativo = true
                });
            }
        }

        if (novasAssociacoes.Any())
        {
            try
            {
                context.PapelPermissoes.AddRange(novasAssociacoes);
                await context.SaveChangesAsync();
                logger.LogInformation($"🔗 {novasAssociacoes.Count} novas permissões associadas ao papel {nomePapel}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"❌ Erro ao associar permissões ao papel {nomePapel}");
            }
        }
        else
        {
            logger.LogInformation($"⚠️ Todas as permissões já estão associadas ao papel {nomePapel}");
        }
    }

    private static async Task CriarGruposBase(GestusDbContexto context, ILogger logger)
    {
        logger.LogInformation("👥 Criando grupos base...");

        var gruposBase = new[]
        {
            new { Nome = "Administradores", Descricao = "Grupo de administradores do sistema", Tipo = "Sistema" },
            new { Nome = "Gestores", Descricao = "Grupo de gestores operacionais", Tipo = "Operacional" },
            new { Nome = "Usuarios", Descricao = "Grupo de usuários padrão", Tipo = "Padrão" },
            new { Nome = "Auditores", Descricao = "Grupo de auditores", Tipo = "Auditoria" }
        };

        foreach (var grupoInfo in gruposBase)
        {
            var grupoExiste = await context.Grupos.AnyAsync(g => g.Nome == grupoInfo.Nome);
            if (!grupoExiste)
            {
                var novoGrupo = new Grupo
                {
                    Nome = grupoInfo.Nome,
                    Descricao = grupoInfo.Descricao,
                    Tipo = grupoInfo.Tipo,
                    Ativo = true,
                    DataCriacao = DateTime.UtcNow
                };

                context.Grupos.Add(novoGrupo);
                logger.LogInformation($"✅ Grupo '{grupoInfo.Nome}' criado");
            }
            else
            {
                logger.LogInformation($"⚠️  Grupo '{grupoInfo.Nome}' já existe");
            }
        }
    }

    private static async Task<Usuario?> CriarAdministrador(UserManager<Usuario> userManager, ILogger logger)
    {
        logger.LogInformation("👤 Criando usuário Administrador...");
        var emailAdmin = "admin@gestus.local";
        var adminExiste = await userManager.FindByEmailAsync(emailAdmin);

        if (adminExiste == null)
        {
            var admin = new Usuario
            {
                UserName = emailAdmin,
                Email = emailAdmin,
                EmailConfirmed = true,
                Nome = "Administrador",
                Sobrenome = "Gestus",
                NomeCompleto = "Administrador",
                Ativo = true,
                DataCriacao = DateTime.UtcNow
            };

            var resultado = await userManager.CreateAsync(admin, "Reboot3!");
            if (resultado.Succeeded)
            {
                logger.LogInformation($"✅ Administrador criado com ID: {admin.Id}");

                var temPapelAdmin = await userManager.IsInRoleAsync(admin, "Admin");
                if (!temPapelAdmin)
                {
                    var resultadoPapel = await userManager.AddToRoleAsync(admin, "Admin");
                    if (!resultadoPapel.Succeeded)
                    {
                        logger.LogError($"❌ Erro ao atribuir papel Admin: {string.Join(", ", resultadoPapel.Errors.Select(e => e.Description))}");
                    }
                }
                return admin;
            }
            else
            {
                logger.LogError($"❌ Erro ao criar Administrador: {string.Join(", ", resultado.Errors.Select(e => e.Description))}");
                return null;
            }
        }
        else
        {
            logger.LogInformation($"⚠️  Administrador já existe (ID: {adminExiste.Id})");
            var temPapel = await userManager.IsInRoleAsync(adminExiste, "Admin");
            if (!temPapel)
            {
                var resultadoPapel = await userManager.AddToRoleAsync(adminExiste, "Admin");
                if (!resultadoPapel.Succeeded)
                {
                    logger.LogError($"❌ Erro ao atribuir papel Admin ao usuário existente: {string.Join(", ", resultadoPapel.Errors.Select(e => e.Description))}");
                }
            }
            return adminExiste;
        }
    }

    private static async Task CriarScopesOpenIddict(IOpenIddictScopeManager scopeManager, ILogger logger)
    {
        logger.LogInformation("🔧 Criando scopes OpenIddict...");

        // Scope profile
        if (await scopeManager.FindByNameAsync("profile") == null)
        {
            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = "profile",
                DisplayName = "Profile",
                Description = "Access to profile information",
                Resources = { "gestus_api" }
            });
            logger.LogInformation("✅ Scope 'profile' criado no OpenIddict");
        }
        else
        {
            logger.LogInformation("⚠️ Scope 'profile' já existe no OpenIddict");
        }

        // Scope email
        if (await scopeManager.FindByNameAsync("email") == null)
        {
            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = "email",
                DisplayName = "Email",
                Description = "Access to email address",
                Resources = { "gestus_api" }
            });
            logger.LogInformation("✅ Scope 'email' criado no OpenIddict");
        }
        else
        {
            logger.LogInformation("⚠️ Scope 'email' já existe no OpenIddict");
        }

        // Scope roles
        if (await scopeManager.FindByNameAsync("roles") == null)
        {
            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = "roles",
                DisplayName = "Roles",
                Description = "Access to user roles",
                Resources = { "gestus_api" }
            });
            logger.LogInformation("✅ Scope 'roles' criado no OpenIddict");
        }
        else
        {
            logger.LogInformation("⚠️ Scope 'roles' já existe no OpenIddict");
        }

        // ✅ CORREÇÃO: Usar OpenIddictConstants.Scopes.OfflineAccess (sem .Permissions.Scopes)
        if (await scopeManager.FindByNameAsync(OpenIddictConstants.Scopes.OfflineAccess) == null)
        {
            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = OpenIddictConstants.Scopes.OfflineAccess,
                DisplayName = "Offline Access",
                Description = "Access to refresh tokens for offline access",
                Resources = { "gestus_api" }
            });
            logger.LogInformation("✅ Scope 'offline_access' criado no OpenIddict");
        }
        else
        {
            logger.LogInformation("⚠️ Scope 'offline_access' já existe no OpenIddict");
        }
    }

    // =========================
    // APLICAÇÕES DO SISTEMA
    // =========================
    private static async Task CriarTiposStatusAplicacaoBase(GestusDbContexto context, ILogger logger)
    {
        logger.LogInformation("🧱 Criando tipos e status de aplicação base...");

        // TipoAplicacao: webapp
        if (!await context.TiposAplicacao.AnyAsync(t => t.Codigo == "webapp"))
        {
            context.TiposAplicacao.Add(new TipoAplicacao
            {
                Codigo = "webapp",
                Nome = "Aplicação Web",
                Descricao = "Aplicação Web baseada em navegador",
                Ordem = 1,
                DataCriacao = DateTime.UtcNow
            });
            logger.LogInformation("✅ TipoAplicacao 'webapp' criado");
        }

        // StatusAplicacao: ativa
        if (!await context.StatusAplicacao.AnyAsync(s => s.Codigo == "ativa"))
        {
            context.StatusAplicacao.Add(new StatusAplicacao
            {
                Codigo = "ativa",
                Nome = "Ativa",
                Descricao = "Aplicação ativa e disponível",
                CorFundo = "#28a745",
                CorTexto = "#ffffff",
                Icone = "✅",
                PermiteAcesso = true,
                PermiteNovoUsuario = true,
                VisivelParaUsuarios = true,
                Padrao = true,
                Ordem = 1,
                DataCriacao = DateTime.UtcNow
            });
            logger.LogInformation("✅ StatusAplicacao 'ativa' criado");
        }

        await context.SaveChangesAsync();
    }

    private static async Task CriarAplicacaoGestus(GestusDbContexto context, IConfiguration configuration, ILogger logger)
    {
        logger.LogInformation("🧩 Criando aplicação 'Gestus'...");

        var baseUrl = configuration["App:BaseUrl"] ?? "https://localhost:7001";
        var tipo = await context.TiposAplicacao.FirstOrDefaultAsync(t => t.Codigo == "webapp");
        var status = await context.StatusAplicacao.FirstOrDefaultAsync(s => s.Codigo == "ativa");

        if (tipo == null || status == null)
        {
            logger.LogWarning("⚠️ Tipo 'webapp' ou Status 'ativa' não encontrados. Pulando criação de aplicação Gestus.");
            return;
        }

        var existe = await context.Aplicacoes.AnyAsync(a => a.Codigo == "gestus");
        if (!existe)
        {
            var app = new Aplicacao
            {
                Nome = "Gestus",
                Codigo = "gestus",
                Descricao = "Plataforma Gestus IAM",
                UrlBase = baseUrl,
                TipoAplicacaoId = tipo.Id,
                StatusAplicacaoId = status.Id,
                Versao = "1.0.0",
                Ativa = true,
                RequerAprovacao = false,
                PermiteAutoRegistro = false,
                NivelSeguranca = 5,
                CriadoPorId = 1,
                DataCriacao = DateTime.UtcNow
            };
            context.Aplicacoes.Add(app);
            await context.SaveChangesAsync();
            logger.LogInformation("✅ Aplicação 'Gestus' criada");
        }
        else
        {
            logger.LogInformation("⚠️ Aplicação 'Gestus' já existe");
        }
    }

    private static async Task ConcederAcessoAplicacaoGestus(GestusDbContexto context, ILogger logger, int? usuarioId)
    {
        if (!usuarioId.HasValue) return;

        var appGestus = await context.Aplicacoes.FirstOrDefaultAsync(a => a.Codigo == "gestus");
        if (appGestus == null) return;

        var existe = await context.UsuariosAplicacao.AnyAsync(ua => ua.UsuarioId == usuarioId.Value && ua.AplicacaoId == appGestus.Id);
        if (!existe)
        {
            context.UsuariosAplicacao.Add(new UsuarioAplicacao
            {
                UsuarioId = usuarioId.Value,
                AplicacaoId = appGestus.Id,
                Aprovado = true,
                DataAprovacao = DateTime.UtcNow,
                Justificativa = "Acesso padrão",
                Ativo = true
            });
            await context.SaveChangesAsync();
            logger.LogInformation("✅ Acesso do usuário {UsuarioId} à aplicação Gestus concedido", usuarioId);
        }
    }
}
