using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Gestus.Modelos;

namespace Gestus.Dados;

public class GestusDbContexto : IdentityDbContext<Usuario, Papel, int, IdentityUserClaim<int>, 
    UsuarioPapel, IdentityUserLogin<int>, IdentityRoleClaim<int>, IdentityUserToken<int>>
{
    public GestusDbContexto(DbContextOptions<GestusDbContexto> options) : base(options)
    {
    }

    // DbSets das entidades personalizadas
    public DbSet<Permissao> Permissoes => Set<Permissao>();
    public DbSet<PapelPermissao> PapelPermissoes => Set<PapelPermissao>();
    public DbSet<Grupo> Grupos => Set<Grupo>();
    public DbSet<UsuarioGrupo> UsuarioGrupos => Set<UsuarioGrupo>();
    public DbSet<RegistroAuditoria> RegistrosAuditoria => Set<RegistroAuditoria>();
    public DbSet<UsuarioPapel> UsuarioPapeis => Set<UsuarioPapel>();
    public DbSet<Papel> Papeis => Set<Papel>();
    public DbSet<ConfiguracaoEmail> ConfiguracoesEmail => Set<ConfiguracaoEmail>();
    public DbSet<TemplateEmail> TemplatesEmail => Set<TemplateEmail>();
    public DbSet<TemplateEmailPersonalizado> TemplatesEmailPersonalizados => Set<TemplateEmailPersonalizado>();
    public DbSet<ChaveEncriptacao> ChavesEncriptacao => Set<ChaveEncriptacao>();
    public DbSet<LogUsoChave> LogsUsoChave => Set<LogUsoChave>();
    public DbSet<Notificacao> Notificacoes => Set<Notificacao>();

    // ✅ ADICIONAR: DbSets para aplicações
    public DbSet<TipoAplicacao> TiposAplicacao => Set<TipoAplicacao>();
    public DbSet<StatusAplicacao> StatusAplicacao => Set<StatusAplicacao>();
    public DbSet<Aplicacao> Aplicacoes => Set<Aplicacao>();
    public DbSet<PermissaoAplicacao> PermissoesAplicacao => Set<PermissaoAplicacao>();
    public DbSet<PapelPermissaoAplicacao> PapelPermissoesAplicacao => Set<PapelPermissaoAplicacao>();
    public DbSet<UsuarioAplicacao> UsuariosAplicacao => Set<UsuarioAplicacao>();
    public DbSet<HistoricoStatusAplicacao> HistoricoStatusAplicacao => Set<HistoricoStatusAplicacao>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configurações das tabelas do Identity
        ConfigurarTabelasIdentity(builder);

        // Configurações das entidades personalizadas
        ConfigurarEntidadesPersonalizadas(builder);

        // ✅ ADICIONAR: Configurações das entidades de aplicação
        ConfigurarEntidadesAplicacao(builder);

        // Configurações de relacionamentos
        ConfigurarRelacionamentos(builder);

        // Configurações de índices
        ConfigurarIndices(builder);

        // Integração com OpenIddict
        builder.UseOpenIddict();

        // Configurações de entidades de email
        ConfigurarEntidadesEmail(builder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {        
        // O OnConfiguring não deve fazer configurações quando já foram feitas no DI
        // As configurações já são feitas em ConfiguracaoEntityFramework.cs
        base.OnConfiguring(optionsBuilder);
    }

    private void ConfigurarTabelasIdentity(ModelBuilder builder)
    {
        // Renomear tabelas do Identity para português
        builder.Entity<Usuario>().ToTable("Usuarios");
        builder.Entity<Papel>().ToTable("Papeis");
        builder.Entity<UsuarioPapel>().ToTable("UsuarioPapeis");
        builder.Entity<IdentityUserClaim<int>>().ToTable("UsuarioReivindicacoes");
        builder.Entity<IdentityUserLogin<int>>().ToTable("UsuarioLogins");
        builder.Entity<IdentityUserToken<int>>().ToTable("UsuarioTokens");
        builder.Entity<IdentityRoleClaim<int>>().ToTable("PapelReivindicacoes");

        // Configurações específicas do Usuario
        builder.Entity<Usuario>(entity =>
        {
            entity.Property(e => e.Nome).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Sobrenome).HasMaxLength(150).IsRequired();
            entity.Property(e => e.NomeCompleto).HasMaxLength(250);
            entity.Property(e => e.Observacoes).HasMaxLength(500);
            entity.Property(e => e.DataCriacao).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Configurações específicas do Papel
        builder.Entity<Papel>(entity =>
        {
            entity.Property(e => e.Descricao).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Categoria).HasMaxLength(100);
            entity.Property(e => e.DataCriacao).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }

    private void ConfigurarEntidadesPersonalizadas(ModelBuilder builder)
    {
        // Configuração da Permissao
        builder.Entity<Permissao>(entity =>
        {
            entity.ToTable("Permissoes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nome).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Descricao).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Recurso).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Acao).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Categoria).HasMaxLength(100);
            entity.Property(e => e.DataCriacao).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Configuração do Grupo
        builder.Entity<Grupo>(entity =>
        {
            entity.ToTable("Grupos");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nome).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Descricao).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Tipo).HasMaxLength(50);
            entity.Property(e => e.DataCriacao).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Configuração do RegistroAuditoria
        builder.Entity<RegistroAuditoria>(entity =>
        {
            entity.ToTable("RegistrosAuditoria");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Acao).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Recurso).HasMaxLength(100).IsRequired();
            entity.Property(e => e.RecursoId).HasMaxLength(50);
            entity.Property(e => e.EnderecoIp).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.Observacoes).HasMaxLength(500);
            entity.Property(e => e.DataHora).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            // ✅ SOLUÇÃO: Usar TEXT em vez de JSONB
            entity.Property(e => e.DadosAntes)
                  .HasColumnType("text")
                  .IsRequired(false);
                  
            entity.Property(e => e.DadosDepois)
                  .HasColumnType("text")
                  .IsRequired(false);
        });

        // Configuração da Notificacao
        builder.Entity<Notificacao>(entity =>
        {
            entity.ToTable("Notificacoes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Titulo).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Mensagem).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.Tipo).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Icone).HasMaxLength(50);
            entity.Property(e => e.Cor).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Origem).HasMaxLength(100);
            entity.Property(e => e.DadosAdicionais).HasColumnType("text");
            entity.Property(e => e.DataCriacao).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasIndex(e => e.UsuarioId);
            entity.HasIndex(e => new { e.UsuarioId, e.Lida });
            entity.HasIndex(e => e.DataCriacao);
            entity.HasIndex(e => e.Tipo);
            entity.HasIndex(e => e.Prioridade);
            
            entity.HasOne(e => e.Usuario)
                  .WithMany()
                  .HasForeignKey(e => e.UsuarioId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    // ✅ ADICIONAR: Configuração das entidades de aplicação
    private void ConfigurarEntidadesAplicacao(ModelBuilder builder)
    {
        // TipoAplicacao
        builder.Entity<TipoAplicacao>(entity =>
        {
            entity.ToTable("TiposAplicacao");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Codigo).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Nome).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Descricao).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Icone).HasMaxLength(50);
            entity.Property(e => e.Cor).HasMaxLength(7);
            entity.Property(e => e.CamposPermissao).HasColumnType("text");
            entity.Property(e => e.SchemaValidacao).HasColumnType("text");
            entity.Property(e => e.TemplatePermissao).HasColumnType("text");
            entity.Property(e => e.ConfiguracoesTipo).HasColumnType("text");
            entity.Property(e => e.InstrucoesIntegracao).HasMaxLength(1000);
            entity.Property(e => e.DataCriacao).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.Codigo).IsUnique();
            entity.HasIndex(e => e.Ordem);

            entity.HasOne(ta => ta.CriadoPor)
                  .WithMany()
                  .HasForeignKey(ta => ta.CriadoPorId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(ta => ta.AtualizadoPor)
                  .WithMany()
                  .HasForeignKey(ta => ta.AtualizadoPorId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // StatusAplicacao
        builder.Entity<StatusAplicacao>(entity =>
        {
            entity.ToTable("StatusAplicacao");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Codigo).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Nome).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Descricao).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CorFundo).HasMaxLength(7).IsRequired();
            entity.Property(e => e.CorTexto).HasMaxLength(7).IsRequired();
            entity.Property(e => e.Icone).HasMaxLength(50);
            entity.Property(e => e.AcoesAutomaticas).HasColumnType("text");
            entity.Property(e => e.MensagemUsuario).HasMaxLength(500);
            entity.Property(e => e.DataCriacao).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.Codigo).IsUnique();
            entity.HasIndex(e => e.Ordem);

            entity.HasOne(sa => sa.CriadoPor)
                  .WithMany()
                  .HasForeignKey(sa => sa.CriadoPorId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(sa => sa.AtualizadoPor)
                  .WithMany()
                  .HasForeignKey(sa => sa.AtualizadoPorId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Aplicacao
        builder.Entity<Aplicacao>(entity =>
        {
            entity.ToTable("Aplicacoes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nome).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Codigo).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Descricao).HasMaxLength(200).IsRequired();
            entity.Property(e => e.UrlBase).HasMaxLength(500);
            entity.Property(e => e.Versao).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ClientId).HasMaxLength(100);
            entity.Property(e => e.ClientSecretEncriptado).HasMaxLength(500);
            entity.Property(e => e.UrlsRedirecionamento).HasColumnType("text");
            entity.Property(e => e.ScopesPermitidos).HasColumnType("text");
            entity.Property(e => e.Configuracoes).HasColumnType("text");
            entity.Property(e => e.MetadadosTipo).HasColumnType("text");
            entity.Property(e => e.Observacoes).HasMaxLength(500);
            entity.Property(e => e.DataCriacao).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.Codigo).IsUnique();
            entity.HasIndex(e => e.ClientId).IsUnique();
            entity.HasIndex(e => new { e.TipoAplicacaoId, e.StatusAplicacaoId });

            entity.HasOne(a => a.TipoAplicacao)
                  .WithMany(ta => ta.Aplicacoes)
                  .HasForeignKey(a => a.TipoAplicacaoId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.StatusAplicacao)
                  .WithMany(sa => sa.Aplicacoes)
                  .HasForeignKey(a => a.StatusAplicacaoId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.CriadoPor)
                  .WithMany()
                  .HasForeignKey(a => a.CriadoPorId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.AtualizadoPor)
                  .WithMany()
                  .HasForeignKey(a => a.AtualizadoPorId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // PermissaoAplicacao
        builder.Entity<PermissaoAplicacao>(entity =>
        {
            entity.ToTable("PermissoesAplicacao");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nome).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Descricao).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Recurso).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Acao).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Endpoint).HasMaxLength(200);
            entity.Property(e => e.MetodoHttp).HasMaxLength(20);
            entity.Property(e => e.Modulo).HasMaxLength(100);
            entity.Property(e => e.Tela).HasMaxLength(100);
            entity.Property(e => e.Comando).HasMaxLength(100);
            entity.Property(e => e.OperacaoSql).HasMaxLength(50);
            entity.Property(e => e.Schema).HasMaxLength(100);
            entity.Property(e => e.Tabela).HasMaxLength(100);
            entity.Property(e => e.Categoria).HasMaxLength(100);
            entity.Property(e => e.Condicoes).HasColumnType("text");
            entity.Property(e => e.DataCriacao).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => new { e.AplicacaoId, e.Recurso, e.Acao }).IsUnique();
            entity.HasIndex(e => new { e.AplicacaoId, e.Categoria });
            entity.HasIndex(e => e.Endpoint);

            entity.HasOne(pa => pa.Aplicacao)
                  .WithMany(a => a.Permissoes)
                  .HasForeignKey(pa => pa.AplicacaoId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pa => pa.AtualizadoPor)
                  .WithMany()
                  .HasForeignKey(pa => pa.AtualizadoPorId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // PapelPermissaoAplicacao
        builder.Entity<PapelPermissaoAplicacao>(entity =>
        {
            entity.ToTable("PapelPermissoesAplicacao");
            entity.HasKey(e => new { e.PapelId, e.PermissaoAplicacaoId });
            entity.Property(e => e.Observacoes).HasMaxLength(500);
            entity.Property(e => e.DataAtribuicao).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => new { e.AplicacaoId, e.PapelId });
            entity.HasIndex(e => e.DataExpiracao);

            entity.HasOne(ppa => ppa.Papel)
                  .WithMany(p => p.PapelPermissoesAplicacao)
                  .HasForeignKey(ppa => ppa.PapelId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ppa => ppa.PermissaoAplicacao)
                  .WithMany(pa => pa.PapelPermissoes)
                  .HasForeignKey(ppa => ppa.PermissaoAplicacaoId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ppa => ppa.Aplicacao)
                  .WithMany()
                  .HasForeignKey(ppa => ppa.AplicacaoId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ppa => ppa.AtribuidoPor)
                  .WithMany()
                  .HasForeignKey(ppa => ppa.AtribuidoPorId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // UsuarioAplicacao
        builder.Entity<UsuarioAplicacao>(entity =>
        {
            entity.ToTable("UsuariosAplicacao");
            entity.HasKey(e => new { e.UsuarioId, e.AplicacaoId });
            entity.Property(e => e.Justificativa).HasMaxLength(500);
            entity.Property(e => e.ObservacoesAprovacao).HasMaxLength(500);
            entity.Property(e => e.ConfiguracoesUsuario).HasColumnType("text");
            entity.Property(e => e.DataSolicitacao).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.DataAprovacao);
            entity.HasIndex(e => e.DataExpiracao);
            entity.HasIndex(e => new { e.AplicacaoId, e.Aprovado });

            entity.HasOne(ua => ua.Usuario)
                  .WithMany(u => u.UsuariosAplicacao)
                  .HasForeignKey(ua => ua.UsuarioId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ua => ua.Aplicacao)
                  .WithMany(a => a.UsuariosAplicacao)
                  .HasForeignKey(ua => ua.AplicacaoId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ua => ua.AprovadoPor)
                  .WithMany()
                  .HasForeignKey(ua => ua.AprovadoPorId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // HistoricoStatusAplicacao
        builder.Entity<HistoricoStatusAplicacao>(entity =>
        {
            entity.ToTable("HistoricoStatusAplicacao");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Motivo).HasMaxLength(500);
            entity.Property(e => e.Observacoes).HasMaxLength(1000);
            entity.Property(e => e.DataMudanca).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => new { e.AplicacaoId, e.DataMudanca });
            entity.HasIndex(e => e.StatusNovoId);

            entity.HasOne(hsa => hsa.Aplicacao)
                  .WithMany(a => a.HistoricoStatus)
                  .HasForeignKey(hsa => hsa.AplicacaoId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(hsa => hsa.StatusAnterior)
                  .WithMany()
                  .HasForeignKey(hsa => hsa.StatusAnteriorId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(hsa => hsa.StatusNovo)
                  .WithMany()
                  .HasForeignKey(hsa => hsa.StatusNovoId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(hsa => hsa.AlteradoPor)
                  .WithMany()
                  .HasForeignKey(hsa => hsa.AlteradoPorId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private void ConfigurarRelacionamentos(ModelBuilder builder)
    {
        // PapelPermissao (many-to-many)
        builder.Entity<PapelPermissao>(entity =>
        {
            entity.ToTable("PapelPermissoes");
            entity.HasKey(e => new { e.PapelId, e.PermissaoId });
            entity.Property(e => e.DataAtribuicao).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(pp => pp.Papel)
                .WithMany(p => p.PapelPermissoes)
                .HasForeignKey(pp => pp.PapelId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pp => pp.Permissao)
                .WithMany(p => p.PapelPermissoes)
                .HasForeignKey(pp => pp.PermissaoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UsuarioGrupo (many-to-many)
        builder.Entity<UsuarioGrupo>(entity =>
        {
            entity.ToTable("UsuarioGrupos");
            entity.HasKey(e => new { e.UsuarioId, e.GrupoId });
            entity.Property(e => e.DataAdesao).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(ug => ug.Usuario)
                .WithMany(u => u.UsuarioGrupos)
                .HasForeignKey(ug => ug.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ug => ug.Grupo)
                .WithMany(g => g.UsuarioGrupos)
                .HasForeignKey(ug => ug.GrupoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ✅ CORRIGIDO: UsuarioPapel - usar propriedades do Identity
        builder.Entity<UsuarioPapel>(entity =>
        {
            entity.Property(e => e.DataAtribuicao).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(up => up.Usuario)
                .WithMany(u => u.UsuarioPapeis)
                .HasForeignKey(up => up.UserId) // ✅ UserId (do Identity)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(up => up.Papel)
                .WithMany(p => p.UsuarioPapeis)
                .HasForeignKey(up => up.RoleId) // ✅ RoleId (do Identity)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(up => up.AtribuidoPor)
                .WithMany()
                .HasForeignKey(up => up.AtribuidoPorId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // RegistroAuditoria -> Usuario
        builder.Entity<RegistroAuditoria>(entity =>
        {
            entity.HasOne(ra => ra.Usuario)
                .WithMany(u => u.RegistrosAuditoria)
                .HasForeignKey(ra => ra.UsuarioId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private void ConfigurarIndices(ModelBuilder builder)
    {
        // Índices para Usuario
        builder.Entity<Usuario>()
               .HasIndex(u => u.Email)
               .IsUnique();

        builder.Entity<Usuario>()
               .HasIndex(u => new { u.Nome, u.Sobrenome });

        builder.Entity<Usuario>()
               .HasIndex(u => u.Ativo);

        // Índices para Permissao
        builder.Entity<Permissao>()
               .HasIndex(p => new { p.Recurso, p.Acao })
               .IsUnique();

        builder.Entity<Permissao>()
               .HasIndex(p => p.Categoria);

        // Índices para Grupo
        builder.Entity<Grupo>()
               .HasIndex(g => g.Nome)
               .IsUnique();

        // Índices para RegistroAuditoria
        builder.Entity<RegistroAuditoria>()
               .HasIndex(ra => ra.DataHora);

        builder.Entity<RegistroAuditoria>()
               .HasIndex(ra => new { ra.Recurso, ra.Acao });

        builder.Entity<RegistroAuditoria>()
               .HasIndex(ra => ra.UsuarioId);

        // ✅ ADICIONAR: Índices para entidades de aplicação
        // TipoAplicacao
        builder.Entity<TipoAplicacao>()
               .HasIndex(ta => ta.Ativo);

        builder.Entity<TipoAplicacao>()
               .HasIndex(ta => ta.NivelComplexidade);

        // StatusAplicacao
        builder.Entity<StatusAplicacao>()
               .HasIndex(sa => sa.PermiteAcesso);

        builder.Entity<StatusAplicacao>()
               .HasIndex(sa => sa.VisivelParaUsuarios);

        // Aplicacao
        builder.Entity<Aplicacao>()
               .HasIndex(a => a.Ativa);

        builder.Entity<Aplicacao>()
               .HasIndex(a => a.NivelSeguranca);

        // PermissaoAplicacao
        builder.Entity<PermissaoAplicacao>()
               .HasIndex(pa => pa.Ativa);

        builder.Entity<PermissaoAplicacao>()
               .HasIndex(pa => pa.Nivel);

        // PapelPermissaoAplicacao
        builder.Entity<PapelPermissaoAplicacao>()
               .HasIndex(ppa => ppa.Ativa);

        // UsuarioAplicacao
        builder.Entity<UsuarioAplicacao>()
               .HasIndex(ua => ua.Ativo);

        builder.Entity<UsuarioAplicacao>()
               .HasIndex(ua => ua.Aprovado);
    }

    private void ConfigurarEntidadesEmail(ModelBuilder builder)
    {
        // ConfiguracaoEmail
        builder.Entity<ConfiguracaoEmail>(entity =>
        {
            entity.ToTable("ConfiguracoesEmail");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ServidorSmtp).HasMaxLength(200).IsRequired();
            entity.Property(e => e.EmailRemetente).HasMaxLength(256).IsRequired();
            entity.Property(e => e.NomeRemetente).HasMaxLength(200).IsRequired();
            entity.Property(e => e.SenhaEncriptada).HasMaxLength(500).IsRequired();
            entity.Property(e => e.DataCriacao).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // TemplateEmail
        builder.Entity<TemplateEmail>(entity =>
        {
            entity.ToTable("TemplatesEmail");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Tipo).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Assunto).HasMaxLength(300).IsRequired();
            entity.Property(e => e.CorpoHtml).HasColumnType("text").IsRequired();
            entity.Property(e => e.CorpoTexto).HasColumnType("text");
            entity.Property(e => e.DataCriacao).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(te => te.ConfiguracaoEmail)
                  .WithMany(ce => ce.Templates)
                  .HasForeignKey(te => te.ConfiguracaoEmailId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // TemplateEmailPersonalizado
        builder.Entity<TemplateEmailPersonalizado>(entity =>
        {
            entity.ToTable("TemplatesEmailPersonalizados");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nome).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Tipo).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Assunto).HasMaxLength(300).IsRequired();
            entity.Property(e => e.CorpoHtml).HasColumnType("text").IsRequired();
            entity.Property(e => e.CorpoTexto).HasColumnType("text");
            entity.Property(e => e.Descricao).HasMaxLength(500);
            entity.Property(e => e.VariaveisObrigatorias).HasColumnType("text");
            entity.Property(e => e.VariaveisOpcionais).HasColumnType("text");
            entity.Property(e => e.DataCriacao).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(tep => tep.CriadoPor)
                  .WithMany()
                  .HasForeignKey(tep => tep.CriadoPorId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(tep => tep.AtualizadoPor)
                  .WithMany()
                  .HasForeignKey(tep => tep.AtualizadoPorId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ChaveEncriptacao
        builder.Entity<ChaveEncriptacao>(entity =>
        {
            entity.ToTable("ChavesEncriptacao");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nome).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ChaveEncriptada).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Observacoes).HasMaxLength(500);
            entity.Property(e => e.DataCriacao).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => new { e.Nome, e.Versao }).IsUnique();
            entity.HasIndex(e => new { e.Nome, e.Ativa });
        });

        // LogUsoChave
        builder.Entity<LogUsoChave>(entity =>
        {
            entity.ToTable("LogsUsoChave");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Operacao).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Contexto).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Identificador).HasMaxLength(200);
            entity.Property(e => e.MensagemErro).HasMaxLength(500);
            entity.Property(e => e.DataHora).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(luc => luc.ChaveEncriptacao)
                  .WithMany()
                  .HasForeignKey(luc => luc.ChaveEncriptacaoId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.DataHora);
            entity.HasIndex(e => new { e.Contexto, e.Operacao });
        });
    }
}