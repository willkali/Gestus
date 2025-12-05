# 🔐 Gestus - Sistema de Gerenciamento de Identidade e Acesso

> **Sistema IAM (Identity & Access Management)** robusto e seguro, construído com .NET 10 e arquitetura em camadas.

## 📋 Sobre

O Gestus é um sistema completo de gerenciamento de identidade e acesso, projetado com foco em **segurança**, **escalabilidade** e **manutenibilidade**.

### ✨ Características Principais

- 🔒 **Segurança por Isolamento Arquitetural** - Arquitetura em camadas (Domain/Application/Infrastructure/API)
- 🌐 **100% em Português** - Código, documentação e padrões em português brasileiro
- 🎯 **Padrões Rigorosos** - Validação automática via CI/CD e Git Hooks
- 🧪 **Testável** - Estrutura preparada para testes unitários e de integração
- 📊 **Auditoria Completa** - Rastreamento de todas as operações críticas
- 🔑 **Controle Granular** - Sistema de permissões flexível e poderoso

## 🏗️ Arquitetura

```
Gestus/
├── Gestus.Domain/          # Núcleo do domínio (entidades, value objects, interfaces)
├── Gestus.Application/     # Lógica de aplicação (serviços, DTOs, validadores)
├── Gestus.Infrastructure/  # Infraestrutura (repositórios, integrações externas)
└── Gestus.Api/            # API REST (controllers, middleware, configuração)
```

## 🚀 Tecnologias

- **.NET 10** - Framework principal
- **C#** - Linguagem de programação
- **PostgreSQL** - Banco de dados
- **Entity Framework Core** - ORM
- **OpenIddict** - OAuth 2.0 / OpenID Connect
- **FluentValidation** - Validação de dados
- **AutoMapper** - Mapeamento de objetos
- **Serilog** - Logging estruturado
- **xUnit** - Framework de testes

## 📝 Padrões de Desenvolvimento

Este projeto segue padrões rigorosos de desenvolvimento:

- ✅ **Nomenclatura em Português** - Classes, métodos e variáveis
- ✅ **Um Arquivo = Uma Classe** - Organização clara
- ✅ **Formatação Allman** - Chaves em linha separada
- ✅ **XML Comments** - Documentação obrigatória
- ✅ **Async/Await** - Sufixo `Async` obrigatório
- ✅ **Segurança** - Nunca logar dados sensíveis

📖 **Documentação completa:** [PADRONIZACAO.md](PADRONIZACAO.md) *(não versionado)*

## 🔧 Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [PostgreSQL 14+](https://www.postgresql.org/download/)
- [Git](https://git-scm.com/downloads)

## 🛠️ Instalação

### 1. Clonar o repositório

```bash
git clone https://github.com/willkali/Gestus.git
cd Gestus
```

### 2. Instalar Git Hooks (Validação Local)

```powershell
.\install-hooks.ps1
```

Isso instalará hooks que validam os padrões **antes de cada commit**.

### 3. Restaurar dependências

```bash
dotnet restore Gestus.sln
```

### 4. Configurar banco de dados

```bash
# Editar connection string em appsettings.json
# Executar migrations
dotnet ef database update --project Gestus.Infrastructure
```

### 5. Executar

```bash
dotnet run --project Gestus.Api
```

A API estará disponível em `https://localhost:7001`

## 🧪 Testes

```bash
# Executar todos os testes
dotnet test Gestus.sln

# Com cobertura
dotnet test Gestus.sln --collect:"XPlat Code Coverage"
```

## 🔍 Validação de Padrões

### Localmente (Git Hooks)

Os hooks validam automaticamente antes de cada commit:
- Formatação de código
- Build sem erros
- Nomenclatura em português
- Estrutura de arquivos
- Segurança

**Bypass (não recomendado):** `git commit --no-verify`

### CI/CD (GitHub Actions)

O pipeline CI/CD executa validações completas em cada push/PR:
- ✅ Formatação
- ✅ Build e análise estática
- ✅ Testes unitários
- ✅ Nomenclatura
- ✅ Estrutura
- ✅ Segurança
- ✅ Documentação

📖 **Documentação:** [CI-CD.md](CI-CD.md) | [GIT-HOOKS.md](GIT-HOOKS.md)

## 📚 Documentação

- [PADRONIZACAO.md](PADRONIZACAO.md) - Padrões de desenvolvimento *(não versionado)*
- [CI-CD.md](CI-CD.md) - Pipeline de CI/CD
- [GIT-HOOKS.md](GIT-HOOKS.md) - Git Hooks e validações locais

## 🤝 Contribuindo

1. Instale os Git Hooks: `.\install-hooks.ps1`
2. Crie uma branch: `git checkout -b feature/minha-feature`
3. Faça suas alterações seguindo os padrões
4. Commit: `git commit -m "feat: adiciona minha feature"`
5. Push: `git push origin feature/minha-feature`
6. Abra um Pull Request

**Importante:** Todos os commits devem passar pelas validações dos hooks e do CI/CD.

## 📄 Licença

Este projeto é proprietário e confidencial.

## 👤 Autor

**William** - [GitHub](https://github.com/willkali)

---

**Status do Projeto:** 🚧 Em Desenvolvimento Ativo

**Última atualização:** Dezembro 2025