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

            // ✅ Verificar se o banco está conectado
            var canConnect = await context.Database.CanConnectAsync();
            logger.LogInformation($"📊 Conexão com banco: {(canConnect ? "OK" : "FALHOU")}");

            if (!canConnect)
            {
                logger.LogError("❌ Não foi possível conectar ao banco de dados. Pulando seeder.");
                return;
            }

            // 1. Criar scopes OpenIddict PRIMEIRO
            logger.LogInformation("🔧 Iniciando criação de scopes OpenIddict...");
            await CriarScopesOpenIddict(scopeManager, logger);

            // 2. Criar aplicações OpenIddict
            logger.LogInformation("🔧 Iniciando criação de aplicações OpenIddict...");
            await CriarAplicacoesOpenIddict(applicationManager, logger);

            // 3. Criar os papéis base
            logger.LogInformation("📋 Iniciando criação de papéis...");
            await CriarPapeisBase(roleManager, logger);

            // 4. Criar as permissões base
            logger.LogInformation("🔐 Iniciando criação de permissões...");
            await CriarPermissoesBase(context, logger);

            // 5. Associar permissões aos papéis
            logger.LogInformation("🔗 Iniciando associação de permissões...");
            await AssociarPermissoesAosPapeis(context, roleManager, logger);

            // 6. Criar usuário Super Admin
            logger.LogInformation("👑 Iniciando criação do Super Admin...");
            await CriarSuperAdmin(userManager, logger);

            // 7. Criar grupos base
            logger.LogInformation("👥 Iniciando criação de grupos...");
            await CriarGruposBase(context, logger);

            // ✅ Salvar todas as mudanças
            logger.LogInformation("💾 Salvando mudanças no banco...");
            var changesSaved = await context.SaveChangesAsync();
            logger.LogInformation($"💾 {changesSaved} mudanças salvas no banco");

            logger.LogInformation("✅ Seeder executado com sucesso!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Erro durante a execução do seeder: {Message}", ex.Message);
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
                    
                    // ✅ CORRIGIDO: Usar scopes que realmente existem
                    OpenIddictConstants.Permissions.Scopes.Email,
                    OpenIddictConstants.Permissions.Scopes.Profile,
                    OpenIddictConstants.Permissions.Scopes.Roles,
                    // ✅ ADICIONADO: Scope customizado para openid via string
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
                    
                    // ✅ CORRIGIDO: Usar scopes que realmente existem
                    OpenIddictConstants.Permissions.Scopes.Email,
                    OpenIddictConstants.Permissions.Scopes.Profile,
                    OpenIddictConstants.Permissions.Scopes.Roles,
                    // ✅ ADICIONADO: Scope customizado para openid via string
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
            
            // Papéis funcionais adicionais
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
                logger.LogInformation($"📋 Criando papel: {papelInfo.Nome}");

                var papel = new Papel
                {
                    Name = papelInfo.Nome,
                    NormalizedName = papelInfo.Nome.ToUpperInvariant(),
                    Descricao = papelInfo.Descricao,
                    Categoria = papelInfo.Categoria,
                    Nivel = papelInfo.Nivel,
                    Ativo = true,
                    DataCriacao = DateTime.UtcNow
                };

                var resultado = await roleManager.CreateAsync(papel);
                if (resultado.Succeeded)
                {
                    logger.LogInformation($"✅ Papel '{papelInfo.Nome}' criado com sucesso (ID: {papel.Id})");
                }
                else
                {
                    logger.LogError($"❌ Erro ao criar papel '{papelInfo.Nome}': {string.Join(", ", resultado.Errors.Select(e => e.Description))}");
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
                    logger.LogError($"❌ Erro específico - Código: {error.Code}, Descrição: {error.Description}");
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

        var permissoesBase = new[]
        {
            // Permissões de Sistema (Super Admin)
            new { Nome = "Sistema.Controle.Total", Descricao = "Controle total do sistema", Recurso = "Sistema", Acao = "Total", Categoria = "Sistema" },
            new { Nome = "Sistema.Configuracao.Gerenciar", Descricao = "Gerenciar configurações do sistema", Recurso = "Sistema", Acao = "Gerenciar", Categoria = "Sistema" },
            
            // Permissões de Usuários
            new { Nome = "Usuarios.Criar", Descricao = "Criar novos usuários", Recurso = "Usuarios", Acao = "Criar", Categoria = "Usuarios" },
            new { Nome = "Usuarios.Listar", Descricao = "Listar usuários", Recurso = "Usuarios", Acao = "Listar", Categoria = "Usuarios" },
            new { Nome = "Usuarios.Visualizar", Descricao = "Visualizar detalhes de usuários", Recurso = "Usuarios", Acao = "Visualizar", Categoria = "Usuarios" },
            new { Nome = "Usuarios.Editar", Descricao = "Editar usuários", Recurso = "Usuarios", Acao = "Editar", Categoria = "Usuarios" },
            new { Nome = "Usuarios.Excluir", Descricao = "Excluir usuários", Recurso = "Usuarios", Acao = "Excluir", Categoria = "Usuarios" },
            new { Nome = "Usuarios.GerenciarPapeis", Descricao = "Gerenciar papéis de usuários", Recurso = "Usuarios", Acao = "GerenciarPapeis", Categoria = "Usuarios" },
            
            // Permissões de Papéis
            new { Nome = "Papeis.Criar", Descricao = "Criar novos papéis", Recurso = "Papeis", Acao = "Criar", Categoria = "Papeis" },
            new { Nome = "Papeis.Listar", Descricao = "Listar papéis", Recurso = "Papeis", Acao = "Listar", Categoria = "Papeis" },
            new { Nome = "Papeis.Visualizar", Descricao = "Visualizar detalhes de papéis", Recurso = "Papeis", Acao = "Visualizar", Categoria = "Papeis" },
            new { Nome = "Papeis.Editar", Descricao = "Editar papéis", Recurso = "Papeis", Acao = "Editar", Categoria = "Papeis" },
            new { Nome = "Papeis.Excluir", Descricao = "Excluir papéis", Recurso = "Papeis", Acao = "Excluir", Categoria = "Papeis" },
            new { Nome = "Papeis.GerenciarPermissoes", Descricao = "Gerenciar permissões de papéis", Recurso = "Papeis", Acao = "GerenciarPermissoes", Categoria = "Papeis" },
            
            // Permissões de Permissões
            new { Nome = "Permissoes.Criar", Descricao = "Criar novas permissões", Recurso = "Permissoes", Acao = "Criar", Categoria = "Permissoes" },
            new { Nome = "Permissoes.Listar", Descricao = "Listar permissões", Recurso = "Permissoes", Acao = "Listar", Categoria = "Permissoes" },
            new { Nome = "Permissoes.Visualizar", Descricao = "Visualizar detalhes de permissões", Recurso = "Permissoes", Acao = "Visualizar", Categoria = "Permissoes" },
            new { Nome = "Permissoes.Editar", Descricao = "Editar permissões", Recurso = "Permissoes", Acao = "Editar", Categoria = "Permissoes" },
            new { Nome = "Permissoes.Excluir", Descricao = "Excluir permissões", Recurso = "Permissoes", Acao = "Excluir", Categoria = "Permissoes" },
            
            // Permissões de Auditoria
            new { Nome = "Auditoria.Visualizar", Descricao = "Visualizar logs de auditoria", Recurso = "Auditoria", Acao = "Visualizar", Categoria = "Auditoria" },
            new { Nome = "Auditoria.Exportar", Descricao = "Exportar dados de auditoria", Recurso = "Auditoria", Acao = "Exportar", Categoria = "Auditoria" },
            
            // Permissões de Grupos
            new { Nome = "Grupos.Criar", Descricao = "Criar novos grupos", Recurso = "Grupos", Acao = "Criar", Categoria = "Grupos" },
            new { Nome = "Grupos.Listar", Descricao = "Listar grupos", Recurso = "Grupos", Acao = "Listar", Categoria = "Grupos" },
            new { Nome = "Grupos.Visualizar", Descricao = "Visualizar detalhes de grupos", Recurso = "Grupos", Acao = "Visualizar", Categoria = "Grupos" },
            new { Nome = "Grupos.Editar", Descricao = "Editar grupos", Recurso = "Grupos", Acao = "Editar", Categoria = "Grupos" },
            new { Nome = "Grupos.Excluir", Descricao = "Excluir grupos", Recurso = "Grupos", Acao = "Excluir", Categoria = "Grupos" }
        };

        foreach (var permissaoInfo in permissoesBase)
        {
            var permissaoExiste = await context.Permissoes
                .AnyAsync(p => p.Recurso == permissaoInfo.Recurso && p.Acao == permissaoInfo.Acao);

            if (!permissaoExiste)
            {
                var permissao = new Permissao
                {
                    Nome = permissaoInfo.Nome,
                    Descricao = permissaoInfo.Descricao,
                    Recurso = permissaoInfo.Recurso,
                    Acao = permissaoInfo.Acao,
                    Categoria = permissaoInfo.Categoria,
                    Ativo = true,
                    DataCriacao = DateTime.UtcNow
                };

                context.Permissoes.Add(permissao);
                logger.LogInformation($"✅ Permissão '{permissaoInfo.Nome}' criada");
            }
            else
            {
                logger.LogInformation($"⚠️  Permissão '{permissaoInfo.Nome}' já existe");
            }
        }

        await context.SaveChangesAsync();
        
        var permissoesCountDepois = await context.Permissoes.CountAsync();
        logger.LogInformation($"🔐 Permissões existentes depois: {permissoesCountDepois}");
    }

    private static async Task AssociarPermissoesAosPapeis(GestusDbContexto context, RoleManager<Papel> roleManager, ILogger logger)
    {
        logger.LogInformation("🔗 Associando permissões aos papéis...");

        // Super Admin: TODAS as permissões
        var superAdminPapel = await roleManager.FindByNameAsync("SuperAdmin");
        if (superAdminPapel != null)
        {
            var todasPermissoes = await context.Permissoes.ToListAsync();
            await AssociarPermissoesAoPapel(context, superAdminPapel.Id, todasPermissoes, logger, "SuperAdmin");
        }
        else
        {
            logger.LogWarning("⚠️  Papel SuperAdmin não encontrado para associar permissões");
        }

        // Admin: Permissões administrativas (exceto sistema total)
        var adminPapel = await roleManager.FindByNameAsync("Admin");
        if (adminPapel != null)
        {
            var permissoesAdmin = await context.Permissoes
                .Where(p => p.Categoria != "Sistema" || p.Acao != "Total")
                .ToListAsync();
            await AssociarPermissoesAoPapel(context, adminPapel.Id, permissoesAdmin, logger, "Admin");
        }

        // Usuario: Apenas permissões de visualização básica
        var usuarioPapel = await roleManager.FindByNameAsync("Usuario");
        if (usuarioPapel != null)
        {
            var permissoesUsuario = await context.Permissoes
                .Where(p => p.Acao == "Listar" || p.Acao == "Visualizar")
                .Where(p => p.Categoria == "Usuarios" || p.Categoria == "Grupos")
                .ToListAsync();
            await AssociarPermissoesAoPapel(context, usuarioPapel.Id, permissoesUsuario, logger, "Usuario");
        }

        // GestorUsuarios: Todas as permissões relacionadas a usuários
        var gestorUsuariosPapel = await roleManager.FindByNameAsync("GestorUsuarios");
        if (gestorUsuariosPapel != null)
        {
            var permissoesGestorUsuarios = await context.Permissoes
                .Where(p => p.Categoria == "Usuarios")
                .ToListAsync();
            await AssociarPermissoesAoPapel(context, gestorUsuariosPapel.Id, permissoesGestorUsuarios, logger, "GestorUsuarios");
        }
        else
        {
            logger.LogWarning("⚠️  Papel GestorUsuarios não encontrado para associar permissões");
        }

        // GestorPermissoes: Permissões para gerenciar papéis e permissões
        var gestorPermissoesPapel = await roleManager.FindByNameAsync("GestorPermissoes");
        if (gestorPermissoesPapel != null)
        {
            var permissoesGestorPermissoes = await context.Permissoes
                .Where(p => p.Categoria == "Papeis" || p.Categoria == "Permissoes")
                .ToListAsync();
            await AssociarPermissoesAoPapel(context, gestorPermissoesPapel.Id, permissoesGestorPermissoes, logger, "GestorPermissoes");
        }
        else
        {
            logger.LogWarning("⚠️  Papel GestorPermissoes não encontrado para associar permissões");
        }

        // Auditor: Apenas permissões de visualização e auditoria
        var auditorPapel = await roleManager.FindByNameAsync("Auditor");
        if (auditorPapel != null)
        {
            var permissoesAuditor = await context.Permissoes
                .Where(p => p.Acao == "Visualizar" || p.Acao == "Listar" || p.Categoria == "Auditoria")
                .ToListAsync();
            await AssociarPermissoesAoPapel(context, auditorPapel.Id, permissoesAuditor, logger, "Auditor");
        }
        else
        {
            logger.LogWarning("⚠️  Papel Auditor não encontrado para associar permissões");
        }

        await context.SaveChangesAsync();
    }

    private static async Task AssociarPermissoesAoPapel(GestusDbContexto context, int papelId, List<Permissao> permissoes, ILogger logger, string nomePapel)
    {
        foreach (var permissao in permissoes)
        {
            var associacaoExiste = await context.PapelPermissoes
                .AnyAsync(pp => pp.PapelId == papelId && pp.PermissaoId == permissao.Id);

            if (!associacaoExiste)
            {
                var papelPermissao = new PapelPermissao
                {
                    PapelId = papelId,
                    PermissaoId = permissao.Id,
                    Ativo = true,
                    DataAtribuicao = DateTime.UtcNow
                };

                context.PapelPermissoes.Add(papelPermissao);
            }
        }

        logger.LogInformation($"🔗 Permissões associadas ao papel '{nomePapel}': {permissoes.Count}");
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
                Description = "Acesso às informações básicas do perfil do usuário",
                Resources =
                {
                    "gestus_api"
                }
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
                Description = "Acesso ao endereço de email do usuário",
                Resources =
                {
                    "gestus_api"
                }
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
                Description = "Acesso aos papéis e permissões do usuário",
                Resources =
                {
                    "gestus_api"
                }
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
            new { Nome = "Administradores", Descricao = "Grupo dos administradores do sistema", Tipo = "Sistema" },
            new { Nome = "Gestores", Descricao = "Grupo dos gestores operacionais", Tipo = "Operacional" },
            new { Nome = "Usuarios", Descricao = "Grupo dos usuários finais", Tipo = "Geral" },
            new { Nome = "Auditores", Descricao = "Grupo dos auditores do sistema", Tipo = "Auditoria" }
        };

        foreach (var grupoInfo in gruposBase)
        {
            var grupoExiste = await context.Grupos.AnyAsync(g => g.Nome == grupoInfo.Nome);
            if (!grupoExiste)
            {
                var grupo = new Grupo
                {
                    Nome = grupoInfo.Nome,
                    Descricao = grupoInfo.Descricao,
                    Tipo = grupoInfo.Tipo,
                    Ativo = true,
                    DataCriacao = DateTime.UtcNow
                };

                context.Grupos.Add(grupo);
                logger.LogInformation($"✅ Grupo '{grupoInfo.Nome}' criado");
            }
            else
            {
                logger.LogInformation($"⚠️  Grupo '{grupoInfo.Nome}' já existe");
            }
        }

        await context.SaveChangesAsync();
    }
}