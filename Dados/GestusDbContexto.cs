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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configurações das tabelas do Identity
        ConfigurarTabelasIdentity(builder);

        // Configurações das entidades personalizadas
        ConfigurarEntidadesPersonalizadas(builder);

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