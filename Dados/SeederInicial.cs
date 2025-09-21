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

        // ✅ ADICIONAR: Gerenciador de aplicações e scopes OpenIddict
        var applicationManager = serviceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var scopeManager = serviceProvider.GetRequiredService<IOpenIddictScopeManager>();

        try
        {
            logger.LogInformation("🌱 Iniciando seeder de dados iniciais...");

            // Verificar conexão
            var canConnect = await context.Database.CanConnectAsync();
            logger.LogInformation($"📊 Conexão com banco: {(canConnect ? "OK" : "FALHOU")}");

            if (!canConnect)
            {
                logger.LogError("❌ Não foi possível conectar ao banco de dados");
                return;
            }

            // 1. Criar scopes OpenIddict
            logger.LogInformation("🔧 Iniciando criação de scopes OpenIddict...");
            await CriarScopesOpenIddict(scopeManager, logger);

            // 2. Criar aplicações OpenIddict
            logger.LogInformation("🔧 Iniciando criação de aplicações OpenIddict...");
            await CriarAplicacoesOpenIddict(applicationManager, logger);

            // 3. Criar papéis base
            logger.LogInformation("📋 Iniciando criação de papéis...");
            await CriarPapeisBase(roleManager, logger);

            // 4. Criar permissões base (incluindo as de email)
            logger.LogInformation("🔐 Iniciando criação de permissões...");
            await CriarPermissoesBase(context, logger);

            // 5. Associar permissões aos papéis
            logger.LogInformation("🔗 Iniciando associação de permissões...");
            await AssociarPermissoesAosPapeis(context, roleManager, logger);

            // 6. Criar usuário super admin
            logger.LogInformation("👑 Iniciando criação do Super Admin...");
            await CriarSuperAdmin(userManager, logger);

            // 7. Criar grupos base
            logger.LogInformation("👥 Iniciando criação de grupos...");
            await CriarGruposBase(context, logger);

            // 8. Salvar mudanças
            logger.LogInformation("💾 Salvando mudanças no banco...");
            var mudancas = await context.SaveChangesAsync();
            logger.LogInformation($"💾 {mudancas} mudanças salvas no banco");

            logger.LogInformation("✅ Seeder executado com sucesso!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Erro durante a execução do seeder");
            throw;
        }
    }

    private static async Task CriarAplicacoesOpenIddict(IOpenIddictApplicationManager applicationManager, ILogger logger)
    {
        logger.LogInformation("🔧 Criando aplicações OpenIddict...");

        // Aplicação para API (Resource Server)
        if (await applicationManager.FindByClientIdAsync("gestus_api") == null)
        {
            await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "gestus_api",
                ClientSecret = "gestus_api_secret_2024",
                ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
                DisplayName = "Gestus API",
                ClientType = OpenIddictConstants.ClientTypes.Confidential,
                Permissions =
                {
                    OpenIddictConstants.Permissions.Endpoints.Token,
                    OpenIddictConstants.Permissions.Endpoints.Introspection,
                    OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
                    OpenIddictConstants.Permissions.GrantTypes.Password,
                    OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                    OpenIddictConstants.Permissions.Scopes.Email,
                    OpenIddictConstants.Permissions.Scopes.Profile,
                    OpenIddictConstants.Permissions.Scopes.Roles,
                    "scp:openid"
                }
            });

            logger.LogInformation("✅ Aplicação 'gestus_api' criada no OpenIddict");
        }
        else
        {
            logger.LogInformation("⚠️ Aplicação 'gestus_api' já existe no OpenIddict");
        }

        // Aplicação para Frontend (SPA)
        if (await applicationManager.FindByClientIdAsync("gestus_spa") == null)
        {
            await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "gestus_spa",
                ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
                DisplayName = "Gestus SPA",
                ClientType = OpenIddictConstants.ClientTypes.Public,
                PostLogoutRedirectUris =
                {
                    new Uri("https://localhost:3000/"),
                    new Uri("http://localhost:3000/")
                },
                RedirectUris =
                {
                    new Uri("https://localhost:3000/callback"),
                    new Uri("http://localhost:3000/callback")
                },
                Permissions =
                {
                    OpenIddictConstants.Permissions.Endpoints.Authorization,
                    OpenIddictConstants.Permissions.Endpoints.Token,
                    OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                    OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                    OpenIddictConstants.Permissions.ResponseTypes.Code,
                    OpenIddictConstants.Permissions.Scopes.Email,
                    OpenIddictConstants.Permissions.Scopes.Profile,
                    OpenIddictConstants.Permissions.Scopes.Roles,
                    "scp:openid"
                },
                Requirements =
                {
                    OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange
                }
            });

            logger.LogInformation("✅ Aplicação 'gestus_spa' criada no OpenIddict");
        }
        else
        {
            logger.LogInformation("⚠️ Aplicação 'gestus_spa' já existe no OpenIddict");
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

    private static async Task CriarSuperAdmin(UserManager<Usuario> userManager, ILogger logger)
    {
        logger.LogInformation("👑 Criando usuário Super Admin...");

        const string emailSuperAdmin = "super@gestus.local";
        const string senhaSuperAdmin = "Reboot3!";

        logger.LogInformation($"👑 Verificando se usuário {emailSuperAdmin} já existe...");
        
        var superAdminExiste = await userManager.FindByEmailAsync(emailSuperAdmin);
        if (superAdminExiste == null)
        {
            logger.LogInformation($"👑 Usuário não existe. Criando {emailSuperAdmin}...");
            
            var superAdmin = new Usuario
            {
                UserName = emailSuperAdmin,
                Email = emailSuperAdmin,
                EmailConfirmed = true,
                Nome = "Super",
                Sobrenome = "Administrador",
                NomeCompleto = "Super Administrador",
                Ativo = true,
                DataCriacao = DateTime.UtcNow,
                Observacoes = "Usuário Super Admin criado automaticamente pelo seeder"
            };

            logger.LogInformation($"👑 Tentando criar usuário...");
            var resultado = await userManager.CreateAsync(superAdmin, senhaSuperAdmin);
            
            if (resultado.Succeeded)
            {
                logger.LogInformation($"✅ Usuário criado com sucesso! ID: {superAdmin.Id}");
                
                // Verificar se o papel SuperAdmin existe
                var papelSuperAdminExiste = await userManager.IsInRoleAsync(superAdmin, "SuperAdmin");
                logger.LogInformation($"🔍 Usuário já tem papel SuperAdmin: {papelSuperAdminExiste}");
                
                if (!papelSuperAdminExiste)
                {
                    logger.LogInformation($"👑 Adicionando papel SuperAdmin ao usuário...");
                    var resultadoPapel = await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
                    
                    if (resultadoPapel.Succeeded)
                    {
                        logger.LogInformation($"✅ Papel SuperAdmin adicionado com sucesso!");
                    }
                    else
                    {
                        logger.LogError($"❌ Erro ao adicionar papel: {string.Join(", ", resultadoPapel.Errors.Select(e => e.Description))}");
                    }
                }
                
                logger.LogInformation($"✅ Super Admin criado: {emailSuperAdmin}");
                logger.LogInformation($"🔑 Credenciais: {emailSuperAdmin} / {senhaSuperAdmin}");
            }
            else
            {
                logger.LogError($"❌ Erro ao criar Super Admin: {string.Join(", ", resultado.Errors.Select(e => e.Description))}");
                
                // Log mais detalhado dos erros
                foreach (var error in resultado.Errors)
                {
                    logger.LogError($"❌ Erro específico: {error.Code} - {error.Description}");
                }
            }
        }
        else
        {
            logger.LogInformation($"⚠️  Super Admin já existe (ID: {superAdminExiste.Id})");
            
            // Verificar se tem o papel correto
            var temPapel = await userManager.IsInRoleAsync(superAdminExiste, "SuperAdmin");
            logger.LogInformation($"🔍 Usuário tem papel SuperAdmin: {temPapel}");
            
            if (!temPapel)
            {
                logger.LogInformation($"👑 Adicionando papel SuperAdmin ao usuário existente...");
                await userManager.AddToRoleAsync(superAdminExiste, "SuperAdmin");
            }
        }
        
        // ✅ Verificação final
        var usuarioFinal = await userManager.FindByEmailAsync(emailSuperAdmin);
        if (usuarioFinal != null)
        {
            var papeis = await userManager.GetRolesAsync(usuarioFinal);
            logger.LogInformation($"✅ Usuário final - ID: {usuarioFinal.Id}, Papéis: {string.Join(", ", papeis)}");
        }
    }

    private static async Task CriarPermissoesBase(GestusDbContexto context, ILogger logger)
    {
        logger.LogInformation("🔐 Criando permissões base...");

        var permissoesCountAntes = await context.Permissoes.CountAsync();
        logger.LogInformation($"🔐 Permissões existentes antes: {permissoesCountAntes}");

        // ✅ PERMISSÕES ATUALIZADAS COM SISTEMA DE EMAIL
        var permissoesBase = new[]
        {
            // Sistema
            new { Nome = "Sistema.Controle.Total", Descricao = "Controle total do sistema", Recurso = "Sistema", Acao = "Controle", Categoria = "Sistema" },
            new { Nome = "Sistema.Configuracao.Gerenciar", Descricao = "Gerenciar configurações do sistema", Recurso = "Sistema", Acao = "Configuracao", Categoria = "Sistema" },
            
            // ✅ NOVAS PERMISSÕES DE EMAIL
            new { Nome = "sistema.email.ler", Descricao = "Visualizar configurações de email", Recurso = "Sistema", Acao = "EmailLer", Categoria = "Email" },
            new { Nome = "sistema.email.configurar", Descricao = "Configurar sistema de email", Recurso = "Sistema", Acao = "EmailConfigurar", Categoria = "Email" },
            new { Nome = "sistema.email.testar", Descricao = "Testar configurações de email", Recurso = "Sistema", Acao = "EmailTestar", Categoria = "Email" },
            
            // ✅ NOVAS PERMISSÕES DE TEMPLATES
            new { Nome = "templates.criar", Descricao = "Criar templates de email", Recurso = "Templates", Acao = "Criar", Categoria = "Email" },
            new { Nome = "templates.editar", Descricao = "Editar templates de email", Recurso = "Templates", Acao = "Editar", Categoria = "Email" },
            new { Nome = "templates.excluir", Descricao = "Excluir templates de email", Recurso = "Templates", Acao = "Excluir", Categoria = "Email" },
            new { Nome = "templates.testar", Descricao = "Testar templates de email", Recurso = "Templates", Acao = "Testar", Categoria = "Email" },
            new { Nome = "sistema.configurar", Descricao = "Configurar sistema", Recurso = "Sistema", Acao = "Configurar", Categoria = "Sistema" },

            // Usuários
            new { Nome = "Usuarios.Criar", Descricao = "Criar novos usuários", Recurso = "Usuarios", Acao = "Criar", Categoria = "Gestão" },
            new { Nome = "Usuarios.Listar", Descricao = "Listar usuários", Recurso = "Usuarios", Acao = "Listar", Categoria = "Gestão" },
            new { Nome = "Usuarios.Visualizar", Descricao = "Visualizar detalhes de usuários", Recurso = "Usuarios", Acao = "Visualizar", Categoria = "Gestão" },
            new { Nome = "Usuarios.Editar", Descricao = "Editar usuários existentes", Recurso = "Usuarios", Acao = "Editar", Categoria = "Gestão" },
            new { Nome = "Usuarios.Excluir", Descricao = "Excluir usuários", Recurso = "Usuarios", Acao = "Excluir", Categoria = "Gestão" },
            new { Nome = "Usuarios.GerenciarPapeis", Descricao = "Gerenciar papéis de usuários", Recurso = "Usuarios", Acao = "GerenciarPapeis", Categoria = "Gestão" },

            // Papéis
            new { Nome = "Papeis.Criar", Descricao = "Criar novos papéis", Recurso = "Papeis", Acao = "Criar", Categoria = "Segurança" },
            new { Nome = "Papeis.Listar", Descricao = "Listar papéis", Recurso = "Papeis", Acao = "Listar", Categoria = "Segurança" },
            new { Nome = "Papeis.Visualizar", Descricao = "Visualizar detalhes de papéis", Recurso = "Papeis", Acao = "Visualizar", Categoria = "Segurança" },
            new { Nome = "Papeis.Editar", Descricao = "Editar papéis existentes", Recurso = "Papeis", Acao = "Editar", Categoria = "Segurança" },
            new { Nome = "Papeis.Excluir", Descricao = "Excluir papéis", Recurso = "Papeis", Acao = "Excluir", Categoria = "Segurança" },
            new { Nome = "Papeis.GerenciarPermissoes", Descricao = "Gerenciar permissões de papéis", Recurso = "Papeis", Acao = "GerenciarPermissoes", Categoria = "Segurança" },

            // Permissões
            new { Nome = "Permissoes.Criar", Descricao = "Criar novas permissões", Recurso = "Permissoes", Acao = "Criar", Categoria = "Segurança" },
            new { Nome = "Permissoes.Listar", Descricao = "Listar permissões", Recurso = "Permissoes", Acao = "Listar", Categoria = "Segurança" },
            new { Nome = "Permissoes.Visualizar", Descricao = "Visualizar detalhes de permissões", Recurso = "Permissoes", Acao = "Visualizar", Categoria = "Segurança" },
            new { Nome = "Permissoes.Editar", Descricao = "Editar permissões existentes", Recurso = "Permissoes", Acao = "Editar", Categoria = "Segurança" },
            new { Nome = "Permissoes.Excluir", Descricao = "Excluir permissões", Recurso = "Permissoes", Acao = "Excluir", Categoria = "Segurança" },

            // Auditoria
            new { Nome = "Auditoria.Visualizar", Descricao = "Visualizar logs de auditoria", Recurso = "Auditoria", Acao = "Visualizar", Categoria = "Auditoria" },
            new { Nome = "Auditoria.Exportar", Descricao = "Exportar logs de auditoria", Recurso = "Auditoria", Acao = "Exportar", Categoria = "Auditoria" },

            // Grupos
            new { Nome = "Grupos.Criar", Descricao = "Criar novos grupos", Recurso = "Grupos", Acao = "Criar", Categoria = "Gestão" },
            new { Nome = "Grupos.Listar", Descricao = "Listar grupos", Recurso = "Grupos", Acao = "Listar", Categoria = "Gestão" },
            new { Nome = "Grupos.Visualizar", Descricao = "Visualizar detalhes de grupos", Recurso = "Grupos", Acao = "Visualizar", Categoria = "Gestão" },
            new { Nome = "Grupos.Editar", Descricao = "Editar grupos existentes", Recurso = "Grupos", Acao = "Editar", Categoria = "Gestão" },
            new { Nome = "Grupos.Excluir", Descricao = "Excluir grupos", Recurso = "Grupos", Acao = "Excluir", Categoria = "Gestão" }
        };

        foreach (var permissaoInfo in permissoesBase)
        {
            var permissaoExiste = await context.Permissoes
                .AnyAsync(p => p.Nome == permissaoInfo.Nome);

            if (!permissaoExiste)
            {
                var novaPermissao = new Permissao
                {
                    Nome = permissaoInfo.Nome,
                    Descricao = permissaoInfo.Descricao,
                    Recurso = permissaoInfo.Recurso,
                    Acao = permissaoInfo.Acao,
                    Categoria = permissaoInfo.Categoria,
                    Ativo = true
                };

                context.Permissoes.Add(novaPermissao);
                logger.LogInformation($"✅ Permissão criada: {permissaoInfo.Nome}");
            }
            else
            {
                logger.LogInformation($"⚠️  Permissão '{permissaoInfo.Nome}' já existe");
            }
        }

        var permissoesCountDepois = await context.Permissoes.CountAsync();
        logger.LogInformation($"🔐 Permissões existentes depois: {permissoesCountDepois}");
    }

    private static async Task AssociarPermissoesAosPapeis(GestusDbContexto context, RoleManager<Papel> roleManager, ILogger logger)
    {
        logger.LogInformation("🔗 Associando permissões aos papéis...");

        var todasPermissoes = await context.Permissoes.Where(p => p.Ativo).ToListAsync();

        // SuperAdmin - TODAS as permissões (incluindo as novas de email)
        var superAdminPapel = await roleManager.FindByNameAsync("SuperAdmin");
        if (superAdminPapel != null)
        {
            await AssociarPermissoesAoPapel(context, superAdminPapel.Id, todasPermissoes, logger, "SuperAdmin");
        }
        else
        {
            logger.LogWarning("⚠️ Papel SuperAdmin não encontrado");
        }

        // Admin - Todas exceto controle total
        var adminPapel = await roleManager.FindByNameAsync("Admin");
        if (adminPapel != null)
        {
            var permissoesAdmin = todasPermissoes
                .Where(p => p.Nome != "Sistema.Controle.Total")
                .ToList();
            await AssociarPermissoesAoPapel(context, adminPapel.Id, permissoesAdmin, logger, "Admin");
        }

        // Usuario - Permissões básicas + visualizar configurações de email
        var usuarioPapel = await roleManager.FindByNameAsync("Usuario");
        if (usuarioPapel != null)
        {
            var permissoesUsuario = todasPermissoes
                .Where(p => p.Nome == "Usuarios.Visualizar" || 
                           p.Nome == "Papeis.Visualizar" || 
                           p.Nome == "Permissoes.Visualizar" ||
                           p.Nome == "sistema.email.ler") // ✅ ADICIONADO
                .ToList();
            await AssociarPermissoesAoPapel(context, usuarioPapel.Id, permissoesUsuario, logger, "Usuario");
        }

        // GestorUsuarios - Gestão de usuários + email
        var gestorUsuariosPapel = await roleManager.FindByNameAsync("GestorUsuarios");
        if (gestorUsuariosPapel != null)
        {
            var permissoesGestorUsuarios = todasPermissoes
                .Where(p => p.Recurso == "Usuarios" || 
                           p.Nome == "sistema.email.ler" ||
                           p.Nome == "templates.testar")
                .ToList();
            await AssociarPermissoesAoPapel(context, gestorUsuariosPapel.Id, permissoesGestorUsuarios, logger, "GestorUsuarios");
        }
        else
        {
            logger.LogWarning("⚠️ Papel GestorUsuarios não encontrado");
        }

        // GestorPermissoes - Gestão de permissões e papéis + email
        var gestorPermissoesPapel = await roleManager.FindByNameAsync("GestorPermissoes");
        if (gestorPermissoesPapel != null)
        {
            var permissoesGestorPermissoes = todasPermissoes
                .Where(p => p.Recurso == "Permissoes" || 
                           p.Recurso == "Papeis" ||
                           p.Nome == "sistema.email.ler" ||
                           p.Nome == "templates.criar" ||
                           p.Nome == "templates.editar")
                .ToList();
            await AssociarPermissoesAoPapel(context, gestorPermissoesPapel.Id, permissoesGestorPermissoes, logger, "GestorPermissoes");
        }
        else
        {
            logger.LogWarning("⚠️ Papel GestorPermissoes não encontrado");
        }

        // Auditor - Apenas visualização + leitura de email
        var auditorPapel = await roleManager.FindByNameAsync("Auditor");
        if (auditorPapel != null)
        {
            var permissoesAuditor = todasPermissoes
                .Where(p => p.Acao == "Visualizar" || 
                           p.Acao == "Listar" || 
                           p.Recurso == "Auditoria" ||
                           p.Nome == "sistema.email.ler")
                .ToList();
            await AssociarPermissoesAoPapel(context, auditorPapel.Id, permissoesAuditor, logger, "Auditor");
        }
        else
        {
            logger.LogWarning("⚠️ Papel Auditor não encontrado");
        }
    }

    private static async Task AssociarPermissoesAoPapel(GestusDbContexto context, int papelId, List<Permissao> permissoes, ILogger logger, string nomePapel)
    {
        var permissoesExistentes = await context.Set<PapelPermissao>()
            .Where(pp => pp.PapelId == papelId)
            .Select(pp => pp.PermissaoId)
            .ToListAsync();

        var novasPermissoes = permissoes
            .Where(p => !permissoesExistentes.Contains(p.Id))
            .ToList();

        foreach (var permissao in novasPermissoes)
        {
            var papelPermissao = new PapelPermissao
            {
                PapelId = papelId,
                PermissaoId = permissao.Id
            };

            context.Set<PapelPermissao>().Add(papelPermissao);
        }

        var totalPermissoes = permissoesExistentes.Count + novasPermissoes.Count;
        logger.LogInformation($"🔗 Permissões associadas ao papel '{nomePapel}': {totalPermissoes}");
    }

    private static async Task CriarScopesOpenIddict(IOpenIddictScopeManager scopeManager, ILogger logger)
    {
        logger.LogInformation("🔧 Criando scopes OpenIddict...");

        if (await scopeManager.FindByNameAsync("profile") == null)
        {
            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = "profile",
                DisplayName = "Profile",
                Description = "Profile information",
                Resources = { "gestus_api" }
            });
            logger.LogInformation("✅ Scope 'profile' criado no OpenIddict");
        }
        else
        {
            logger.LogInformation("⚠️ Scope 'profile' já existe no OpenIddict");
        }

        if (await scopeManager.FindByNameAsync("email") == null)
        {
            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = "email",
                DisplayName = "Email",
                Description = "Email address",
                Resources = { "gestus_api" }
            });
            logger.LogInformation("✅ Scope 'email' criado no OpenIddict");
        }
        else
        {
            logger.LogInformation("⚠️ Scope 'email' já existe no OpenIddict");
        }

        if (await scopeManager.FindByNameAsync("roles") == null)
        {
            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = "roles",
                DisplayName = "Roles",
                Description = "User roles and permissions",
                Resources = { "gestus_api" }
            });
            logger.LogInformation("✅ Scope 'roles' criado no OpenIddict");
        }
        else
        {
            logger.LogInformation("⚠️ Scope 'roles' já existe no OpenIddict");
        }
    }

    private static async Task CriarGruposBase(GestusDbContexto context, ILogger logger)
    {
        logger.LogInformation("👥 Criando grupos base...");

        var gruposBase = new[]
        {
            new { Nome = "Administradores", Descricao = "Grupo de administradores do sistema", Tipo = "Sistema" },
            new { Nome = "Gestores", Descricao = "Grupo de gestores operacionais", Tipo = "Operacional" },
            new { Nome = "Usuarios", Descricao = "Grupo de usuários padrão", Tipo = "Geral" },
            new { Nome = "Auditores", Descricao = "Grupo de auditores", Tipo = "Funcional" }
        };

        foreach (var grupoInfo in gruposBase)
        {
            var grupoExiste = await context.Grupos
                .AnyAsync(g => g.Nome == grupoInfo.Nome);

            if (!grupoExiste)
            {
                var novoGrupo = new Grupo
                {
                    Nome = grupoInfo.Nome,
                    Descricao = grupoInfo.Descricao,
                    Tipo = grupoInfo.Tipo,
                    Ativo = true
                };

                context.Grupos.Add(novoGrupo);
                logger.LogInformation($"✅ Grupo criado: {grupoInfo.Nome}");
            }
            else
            {
                logger.LogInformation($"⚠️  Grupo '{grupoInfo.Nome}' já existe");
            }
        }
    }
}