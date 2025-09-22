# Gestus - Backend Identity & Access Management (IAM)

## Visão Geral

Gestus é um backend corporativo para **Identity & Access Management (IAM)**, projetado para centralizar e orquestrar autenticação, autorização, permissões granularizadas, auditoria e monitoramento em múltiplas aplicações. O foco é fornecer uma base robusta, segura e escalável, facilitando a governança de identidade e acesso para ambientes críticos.

---

## Arquitetura e Camadas

O projeto segue o padrão **MVC**, com separação rigorosa entre domínio, infraestrutura e apresentação:

- **Controllers:** Exposição de endpoints RESTful, segregados por responsabilidade (usuários, papéis, permissões, autenticação, auditoria, etc).
- **Models:** Entidades de domínio fortemente tipadas, representando usuários, papéis, permissões, grupos e logs de auditoria. Utilizam anotações para validação e integração com o ORM.
- **DTOs/ViewModels:** Objetos de transferência para entrada/saída nas APIs, desacoplando os modelos internos das estruturas expostas.
- **Repositories:** Interfaces e implementações para abstração do acesso aos dados, facilitando testes, manutenção e evolução.
- **Services:** Camada de lógica de negócio, responsável por processar regras, validações e integrações entre componentes.
- **Middlewares:** Pipeline para logging, tratamento global de exceções, autenticação/autorização customizada, CORS, etc.
- **Configurations:** Centralização de parâmetros de configuração (Swagger, Identity, Serilog, SpiceDB, JWT, Prometheus, etc).
- **HealthChecks:** Implementações para verificação de saúde de banco de dados, SpiceDB, endpoints externos, entre outros.
- **Mappings:** Perfis do AutoMapper para conversão automática entre Models, DTOs e ViewModels.
- **Extensions:** Métodos utilitários para funcionalidades transversais.

---

## Tecnologias, Bibliotecas e Integrações

- **ASP.NET Core 8:** Web API, DI, Middlewares, DataProtection, CORS.
- **Entity Framework Core + PostgreSQL:** ORM, migrations, seeding automatizado, conexões resilientes.
- **OpenIddict + ASP.NET Core Identity:** Implementação de OpenID Connect/OAuth2, autenticação via JWT, gestão de papéis, claims, login social, PKCE, etc.
- **SpiceDB/Authzed:** Permissões granularizadas via gRPC, suporte a RBAC/ABAC, controle dinâmico, relações complexas.
- **Serilog:** Logging estruturado, sinks para console, arquivos, enriquecimento de contexto, rastreabilidade.
- **Prometheus-net.AspNetCore:** Exposição de métricas HTTP, monitoração customizada, counters, histograms.
- **HealthChecks (UI/Npgsql/Uris):** Monitoramento de banco, endpoints, serviços externos, UI integrada.
- **Swagger/Swashbuckle:** Documentação automática dos endpoints, autenticação via JWT, exemplos interativos.
- **AutoMapper:** Mapeamento inteligente entre camadas de dados.
- **FluentValidation:** Validação robusta de entrada e saída, regras customizadas, mensagens amigáveis.
- **Polly:** Resiliência em chamadas externas, políticas de retry, circuit breaker e timeout.

---

## Fluxos e Funcionalidades

### Autenticação

- Emissão de tokens JWT via OpenIddict, com suporte a diversos fluxos OAuth2 (Authorization Code, Client Credentials, Password, Refresh Token).
- Gestão de usuários, papéis, claims e permissões via ASP.NET Core Identity.
- Integração com login social e PKCE para aplicações SPA.

### Autorização Granularizada

- Controle de acesso baseado em relações, usando SpiceDB/Authzed.
- Permissões dinâmicas (RBAC, ABAC), hierarquia de papéis, grupos e permissões customizadas.
- Auditoria de acessos e modificações em tempo real.

### Auditoria e Logging

- Registro detalhado de todas operações críticas e eventos sensíveis.
- Logs enriquecidos com contexto de usuário, requestId, IP, device, etc.
- Persistência de logs para análise posterior e conformidade.

### Monitoramento

- Exposição de métricas detalhadas via Prometheus.
- HealthChecks periódicos em bancos, serviços de permissões, endpoints externos.
- UI integrada para acompanhamento visual do status dos serviços.

### Provisionamento Inicial

- Seeder automatizado para criação de usuários, papéis, permissões padrão, grupos e templates de e-mail (onboarding, recuperação de senha, confirmação de e-mail).
- Configuração automática de clientes OAuth2 (API e SPA), scopes customizados, redirect URIs, requisitos de segurança (PKCE).
  
---

## Requisitos de Ambiente

1. **.NET 8 SDK**
2. **PostgreSQL** (configuração em `appsettings.json`)
3. **SpiceDB** (endpoint e token configurados)
4. **Prometheus** para coleta de métricas (opcional)
5. **Docker** (opcional, para facilitar setup dos serviços)

---

## Instalação e Execução

1. Clone o repositório.
2. Configure os arquivos `appsettings.json` e `appsettings.Development.json` conforme ambiente.
3. Execute as migrations do banco:
   ```
   dotnet ef database update
   ```
4. Inicie a aplicação:
   ```
   dotnet run
   ```
5. Acesse a documentação interativa dos endpoints via Swagger em `/swagger`.

---

## Boas Práticas

- Siga rigorosamente o padrão arquitetural MVC e a organização proposta.
- Utilize DI para todos os componentes e prefira interfaces para facilitar testes e manutenção.
- Centralize regras de validação em DTOs e ViewModels.
- Amplie HealthChecks e monitoramento conforme novas dependências externas.
- Expanda integrações usando Polly para garantir resiliência.
- Nunca exponha dados sensíveis em logs (senhas, tokens, etc).
- Implemente testes unitários e de integração para lógica de negócio.

---

## Segurança

- Todas rotas sensíveis exigem autenticação via JWT.
- Permissões são validadas de forma granular por SpiceDB/Authzed.
- DataProtection, CORS e políticas restritivas estão habilitadas por padrão.
- Auditoria completa de todas operações críticas para conformidade.

---

## Referências

- [ASP.NET Core Identity](https://learn.microsoft.com/aspnet/core/security/authentication/identity)
- [OpenIddict](https://documentation.openiddict.com/)
- [SpiceDB/Authzed](https://authzed.com/docs/spicedb/)
- [Serilog](https://serilog.net/)
- [Prometheus-net](https://github.com/prometheus-net/prometheus-net)
- [Swagger/Swashbuckle](https://github.com/domaindrivendev/Swashbuckle.AspNetCore)
- [AutoMapper](https://automapper.org/)
- [FluentValidation](https://fluentvalidation.net/)

---

## Licença

MIT

---

## Autor

Desenvolvido por [willkali](https://github.com/willkali)
