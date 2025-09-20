# Projeto Gestus - Backend IAM

## Contexto & Objetivo

Estou iniciando o desenvolvimento do backend de um sistema de IAM (Identity & Access Management) chamado **Gestus**.  
O objetivo é centralizar autenticação, autorização, permissões granularizadas e auditoria para aplicações corporativas, com alta segurança, escalabilidade e monitoramento.

---

## Tecnologias e Bibliotecas

- **ASP.NET Core Web API** (MVC)
- **Entity Framework Core** + **PostgreSQL**
- **OpenIddict** (OpenID Connect/OAuth2)
- **ASP.NET Core Identity**
- **Authzed.Api.V1** (SpiceDB para permissões granularizadas via gRPC)
- **Serilog** (logging e auditoria)
- **Prometheus-net.AspNetCore** (monitoramento/métricas)
- **HealthChecks** (UI, Npgsql, Uris)
- **Swagger (Swashbuckle.AspNetCore)** (documentação automática)
- **AutoMapper** (mapeamento entre Models, DTOs, ViewModels)
- **FluentValidation** (validação de dados)
- **Polly** (resiliência)
- **CORS, DataProtection, JwtBearer, Cookies, etc**

---

## Estrutura de Pastas (MVC Adaptada)

```
Gestus/
├── Controllers/           # Endpoints REST (ex: UsersController, AuthController, PermissionsController)
├── Models/                # Entidades do domínio (User, Role, Permission, Group, AuditLog)
├── Data/                  # DbContext, Migrations, Seeders
├── Repositories/          # Interfaces/implementações de acesso a dados
├── Services/              # Lógica de negócio
├── DTOs/                  # Objetos para entrada/saída da API
├── ViewModels/            # Modelos para resposta da API
├── Mappings/              # Perfis do AutoMapper
├── Middlewares/           # Middlewares personalizados (Logging, ExceptionHandler, Authorization)
├── Extensions/            # Métodos de extensão, utilitários
├── Configurations/        # Configuração de Swagger, Identity, Serilog, SpiceDB, etc
├── HealthChecks/          # Health checks customizados (DB, SpiceDB, etc)
├── Properties/            # launchSettings.json
├── wwwroot/               # Arquivos estáticos (se necessário)
├── appsettings.json       # Configuração principal
├── appsettings.Development.json
├── Gestus.csproj          # Manifesto do projeto e dependências
├── Program.cs             # Entry point
├── Startup.cs             # Configuração dos serviços e pipeline
└── README.md              # Documentação do projeto
```

---

## .csproj Base

O arquivo abaixo já inclui todos os pacotes essenciais para o cenário IAM desenhado.

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <AssemblyName>Gestus</AssemblyName>
    <RootNamespace>Gestus</RootNamespace>
    <Configurations>Debug;Release</Configurations>
    <LangVersion>latest</LangVersion>
    <NoWarn>$(NoWarn);1591</NoWarn>
  </PropertyGroup>
  <ItemGroup>
    <!-- ASP.NET Core Web API & Utilitários -->
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="8.0.20" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="8.1.4" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="9.0.9" />
    <PackageReference Include="Microsoft.Extensions.Http" Version="9.0.9" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="9.0.9" />
    <PackageReference Include="Microsoft.AspNetCore.Cors" Version="2.3.0" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.NewtonsoftJson" Version="8.0.20" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.20" />

    <!-- Entity Framework Core + PostgreSQL -->
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.9" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.9">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.20">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.0.4" />

    <!-- Autenticação e Identidade (corrigido para versão compatível) -->
    <PackageReference Include="OpenIddict.AspNetCore" Version="7.1.0" />
    <PackageReference Include="OpenIddict.EntityFrameworkCore" Version="7.1.0" />
    <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="8.0.20" />
    <PackageReference Include="Microsoft.AspNetCore.Identity.UI" Version="8.0.20" />
    <PackageReference Include="OpenIddict.Client.WebIntegration" Version="7.1.0" />

    <!-- Permissões granularizadas (SpiceDB/Authzed) -->
    <PackageReference Include="Authzed.Net" Version="1.4.0" />

    <!-- Logging/Auditoria -->
    <PackageReference Include="Serilog.AspNetCore" Version="9.0.0" />
    <PackageReference Include="Serilog.Sinks.File" Version="7.0.0" />
    <PackageReference Include="Serilog.Sinks.Console" Version="6.0.0" />
    <PackageReference Include="Serilog.Enrichers.Thread" Version="4.0.0" />
    <PackageReference Include="Serilog.Enrichers.Process" Version="3.0.0" />

    <!-- Monitoramento -->
    <PackageReference Include="prometheus-net.AspNetCore" Version="8.2.1" />

    <!-- Health Checks -->
    <PackageReference Include="AspNetCore.HealthChecks.UI" Version="9.0.0" />
    <PackageReference Include="AspNetCore.HealthChecks.UI.PostgreSQL.Storage" Version="9.0.0" />
    <PackageReference Include="AspNetCore.HealthChecks.NpgSql" Version="9.0.0" />
    <PackageReference Include="AspNetCore.HealthChecks.Uris" Version="9.0.0" />

    <!-- Segurança extra -->
    <PackageReference Include="Microsoft.AspNetCore.DataProtection" Version="8.0.20" />

    <!-- Outras utilidades para futuras integrações -->
    <PackageReference Include="FluentValidation.AspNetCore" Version="11.3.1" />
    <PackageReference Include="AutoMapper" Version="13.0.1" />
    <PackageReference Include="Polly" Version="8.6.3" />
    <PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="9.9.0" />
    <PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.14.0" />
  </ItemGroup>
</Project>
```

---

## Instruções para o Copilot Chat

- Considere todo o contexto, arquitetura e pacotes acima.
- Siga a estrutura de pastas proposta.
- Sempre utilize boas práticas de segurança, modularidade e escalabilidade.
- Implemente exemplos de Controllers, Models, Repositories, Services, DTOs, Middlewares, Configurations conforme solicitado.
- Configure autenticação (OpenIddict + Identity + JWT), permissões granularizadas (SpiceDB/Authzed), logs (Serilog), monitoramento (Prometheus), health checks e Swagger.
- Caso surjam necessidades adicionais, sugira novos pacotes ou estruturas.

---

**A partir deste contexto, ajude a gerar e refinar o código do backend Gestus conforme solicitado.**
