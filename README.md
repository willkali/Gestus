# Gestus IAM

API em ASP.NET Core 8 para autenticação/autorização (OpenIddict + ASP.NET Identity), controle granular de permissões, auditoria e integrações de email. Banco de dados PostgreSQL via Entity Framework Core. Observabilidade com Serilog, HealthChecks e Prometheus.


## Requisitos

- .NET 8 SDK
- PostgreSQL (local ou remoto)
- Ferramenta de migrations do EF Core
  - Instalação/atualização (PowerShell):
    - `dotnet tool update -g dotnet-ef`


## Configuração

1) Variáveis de ambiente (opcional, recomendado)
- Ambiente de desenvolvimento:
  - PowerShell: `$env:ASPNETCORE_ENVIRONMENT = "Development"`
- URLs do Kestrel (produção, exemplo):
  - PowerShell: `$env:ASPNETCORE_URLS = "http://0.0.0.0:5000;https://0.0.0.0:7001"`

2) appsettings (ajuste conforme seu ambiente)
- `appsettings.json`
  - ConnectionStrings.DefaultConnection (PostgreSQL)
  - App.BaseUrl (URL pública da API; usada em emails/templates)
  - Cors.AllowedOrigins (origens permitidas)
- `appsettings.Development.json`
  - Já configurado para escutar em `http://0.0.0.0:5000`
- OpenIddict (clients, URIs e segredos) — parametrizável por appsettings/variáveis de ambiente:
  - `OpenIddict:Api:ClientId` (default: gestus_api)
  - `OpenIddict:Api:ClientSecret` (defina no ambiente)
  - `OpenIddict:Spa:ClientId` (default: gestus_spa)
  - `OpenIddict:Spa:RedirectUris` (lista, ex.: `"http://SEU_IP:3000/callback"`)
  - `OpenIddict:Spa:PostLogoutRedirectUris` (lista)

Exemplos (PowerShell, dev):
- `$env:OpenIddict__Api__ClientId = "gestus_api"`
- `$env:OpenIddict__Api__ClientSecret = "{{CLIENT_SECRET}}"`
- `$env:OpenIddict__Spa__ClientId = "gestus_spa"`
- `$env:OpenIddict__Spa__RedirectUris = "[\"http://192.168.0.10:3000/callback\"]"`
- `$env:OpenIddict__Spa__PostLogoutRedirectUris = "[\"http://192.168.0.10:3000/\"]"`


## Banco de dados (criação e atualização)

Use o contexto `GestusDbContexto`.

1) Build do projeto
- `dotnet build`

2) Gerar migrations (se necessário)
- `dotnet ef migrations add InitFullSchema -c GestusDbContexto`

3) Aplicar migrations no banco
- `dotnet ef database update --context GestusDbContexto`

4) (Opcional, DEV) Recriar banco do zero
- `dotnet ef database drop -f --context GestusDbContexto`
- `dotnet ef database update --context GestusDbContexto`


## Executar a API

- `dotnet run`
- Swagger/UI: `/` (raiz)
- Healthcheck JSON: `/saude`
- HealthChecks UI: `/saude-ui`
- Métricas Prometheus: `/metrics` (conforme pipeline)


## Seeder (dados iniciais)

Executado no startup e idempotente. Cria/garante:
- Scopes e aplicações OpenIddict (conforme configuração)
- Papéis: SuperAdmin, Admin, Usuario, GestorUsuarios, GestorPermissoes, Auditor
- Permissões por recurso/ação
- Associação de permissões:
  - SuperAdmin: bypass total (não recebe permissões explícitas)
  - Admin: todas, exceto gestão de papéis (criar/editar/remover/gerenciar) e exclusão permanente de usuários
- Aplicação “Gestus” (webapp/ativa) e acesso aprovado para SuperAdmin/Admin
- Templates de email padrão (configuração de email só é criada automaticamente se não existir nenhuma — ativa ou inativa)

Contas padrão:
- Super Admin
  - Email: `willian.cavalcante@skymsen.com`
  - Senha: `Reboot3!`
- Administrador
  - Email: `super@gestus.local`
  - Senha: `Reboot3!`


## Autenticação

- Token endpoint OpenIddict: `POST /connect/token`
  - Grant types: password, authorization_code (SPA), refresh_token, client_credentials
- Exemplo (password grant) — dev (PowerShell):
```
$CLIENT_ID = "gestus_api"
$CLIENT_SECRET = "{{CLIENT_SECRET}}"   # defina no ambiente
$BODY = @{
  grant_type = "password"
  client_id = $CLIENT_ID
  client_secret = $CLIENT_SECRET
  username = "willian.cavalcante@skymsen.com"
  password = "Reboot3!"
  scope = "openid profile email roles"
}
Invoke-RestMethod -Method Post -Uri "http://localhost:5000/connect/token" -Body $BODY
```
- Login “amigável”: `POST /api/autenticacao/login` (encapsula `/connect/token`)
- Bypass SuperAdmin:
  - Tokens do SuperAdmin incluem `permissao="*"`
  - AuthorizationHandler central concede acesso se `SuperAdmin` ou `*`


## Configuração de Email

- Endpoint: `api/email-config`
- Inicialização cria templates padrão; a configuração SMTP só é criada automaticamente se não existir nenhuma (ativa ou inativa). Configure via API em produção.


## Desenvolvimento em rede

- Em dev, API escuta em `0.0.0.0:5000`. Ajuste CORS (`Cors:AllowedOrigins`) e OpenIddict:Spa:* para refletir o host/IP do seu frontend.