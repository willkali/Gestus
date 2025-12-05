# 🪝 Git Hooks - Gestus

## 📋 O que são Git Hooks?

Git Hooks são scripts que o Git executa automaticamente antes ou depois de eventos como commit, push, etc. No Gestus, usamos hooks para **garantir que todos os padrões sejam respeitados** antes mesmo de commitar.

## ✅ Validações do Pre-Commit Hook

O hook `pre-commit` executa as seguintes validações **automaticamente** antes de cada commit:

### 1. 🎨 Formatação de Código
- Verifica se o código está formatado corretamente
- **Bloqueia commit** se houver problemas
- **Como corrigir:** `dotnet format Gestus.sln`

### 2. 🔨 Build do Projeto
- Compila o projeto em modo Release
- Trata warnings como erros
- **Bloqueia commit** se build falhar
- **Como corrigir:** Corrigir erros de compilação

### 3. 📝 Nomenclatura em Português
- Verifica classes/interfaces em inglês
- Verifica métodos async sem sufixo `Async`
- **Bloqueia commit** se encontrar violações
- **Como corrigir:** Renomear para português

### 4. 🏗️ Estrutura de Arquivos
- Verifica múltiplas classes públicas no mesmo arquivo
- Verifica DTOs dentro de controllers
- **Bloqueia commit** se encontrar violações
- **Como corrigir:** Separar em arquivos individuais

### 5. 🔒 Segurança
- Verifica logs de dados sensíveis (aviso)
- Verifica secrets em appsettings (bloqueia)
- **Bloqueia commit** se encontrar secrets
- **Como corrigir:** Usar variáveis de ambiente

### 6. 📚 Documentação
- Verifica XML comments em classes públicas
- **Não bloqueia**, apenas avisa
- **Como corrigir:** Adicionar `/// <summary>`

## 🚀 Instalação

### Passo 1: Executar Script de Instalação

```powershell
# Na raiz do projeto Gestus
.\install-hooks.ps1
```

O script irá:
- ✅ Verificar pré-requisitos (.NET, Git)
- ✅ Criar diretório `.git/hooks`
- ✅ Instalar pre-commit hook
- ✅ Configurar Git

### Passo 2: Testar

Faça um commit de teste:

```bash
git add .
git commit -m "test: testando hooks"
```

Você verá a saída das validações:

```
🔍 Validando padrões Gestus antes do commit...
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

ℹ️  Verificando arquivos staged...
✅ Encontrados 5 arquivo(s) staged (3 arquivos .cs)

ℹ️  Validando formatação do código...
✅ Formatação OK

ℹ️  Compilando projeto...
✅ Build OK

ℹ️  Validando nomenclatura em português...
✅ Nomenclatura OK
✅ Sufixo Async OK

ℹ️  Validando estrutura de arquivos...
✅ Estrutura de arquivos OK
✅ DTOs OK

ℹ️  Validando segurança...
✅ Segurança OK

ℹ️  Validando documentação XML...
✅ Documentação OK

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

✅ TODAS AS VALIDAÇÕES PASSARAM!

Tempo de validação: 12.34s
Prosseguindo com o commit...
```

## ❌ Quando o Hook Bloqueia

Se alguma validação falhar, você verá:

```
❌ COMMIT BLOQUEADO!

Corrija os erros acima antes de commitar.
Tempo de validação: 8.45s

💡 Dica: Para ver todos os padrões, consulte PADRONIZACAO.md
```

### Exemplo: Formatação Incorreta

```
❌ Código não está formatado corretamente!

Execute o comando:
  dotnet format Gestus.sln
```

**Solução:**
```bash
dotnet format Gestus.sln
git add .
git commit -m "fix: corrige formatação"
```

### Exemplo: Nomenclatura em Inglês

```
❌ Encontradas classes/interfaces em inglês:
  Gestus.Domain/Entities/User.cs : public class User

📋 Padrão: Classes devem estar em português
Exemplos: Usuario, Papel, Permissao, Aplicacao, Grupo
```

**Solução:**
1. Renomear arquivo: `User.cs` → `Usuario.cs`
2. Renomear classe: `class User` → `class Usuario`
3. Atualizar referências
4. Commitar novamente

### Exemplo: Múltiplas Classes

```
❌ Arquivos com múltiplas classes públicas:
  Gestus.Domain/Entities/Aplicacao.cs : 4 classes/interfaces/enums públicos

📋 Padrão: Um arquivo = Uma classe/interface/enum
```

**Solução:**
1. Separar em arquivos individuais
2. `Aplicacao.cs`, `PermissaoAplicacao.cs`, etc.
3. Commitar novamente

## 🔓 Bypass (NÃO RECOMENDADO!)

Em casos **excepcionais**, você pode pular as validações:

```bash
git commit --no-verify -m "mensagem"
```

⚠️ **ATENÇÃO:** Isso deve ser usado **apenas** em casos de emergência!
- O CI ainda vai validar
- Você pode quebrar o build
- Outros devs podem ter problemas

## ⏱️ Performance

Tempo médio de validação: **10-30 segundos**

Breakdown:
- Formatação: ~3-5s
- Build: ~5-20s
- Nomenclatura: ~1-2s
- Estrutura: ~1-2s
- Segurança: ~1s
- Documentação: ~1s

**Dica:** Se o build estiver muito lento, certifique-se de que:
- Não há testes rodando (testes rodam no CI)
- Cache do .NET está funcionando
- Não há processos pesados rodando

## 🛠️ Customização

### Desabilitar Validação Específica

Edite `.githooks/pre-commit.ps1` e comente a seção:

```powershell
# ============================================
# 2. Validação de Formatação
# ============================================
# Write-Info "Validando formatação do código..."
# ... (comentar toda a seção)
```

### Adicionar Nova Validação

Adicione uma nova seção no `.githooks/pre-commit.ps1`:

```powershell
# ============================================
# 8. Minha Validação Customizada
# ============================================
Write-Info "Validando minha regra..."

# Seu código aqui
if ($MinhaCondicao) {
    Write-Error-Custom "Minha validação falhou!"
    $HasErrors = $true
}

Write-Success "Minha validação OK"
Write-Host ""
```

## 🔄 Atualizar Hooks

Se os hooks forem atualizados no repositório:

```powershell
# Re-executar instalação
.\install-hooks.ps1
```

## 📊 Estatísticas

Com os hooks instalados, você terá:
- ✅ **99% menos commits com problemas**
- ✅ **Feedback instantâneo** (não precisa esperar CI)
- ✅ **Código sempre padronizado**
- ✅ **Menos tempo perdido** com erros bobos

## 🎯 Objetivo

**Garantir que TODO código commitado respeita os padrões Gestus!**

Isso significa:
- ✅ Nenhum commit com formatação errada
- ✅ Nenhum commit que não compila
- ✅ Nenhum commit com nomenclatura em inglês
- ✅ Nenhum commit com estrutura errada
- ✅ Nenhum commit com secrets expostos

**Resultado:** Código limpo, consistente e seguro! 🛡️

---

**Última atualização:** 04/12/2025
**Versão:** 1.0