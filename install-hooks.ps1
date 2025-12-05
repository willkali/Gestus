# ============================================
# Instalador de Git Hooks - Gestus
# ============================================
# Este script instala os hooks de validação automaticamente

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "🔧 Instalando Git Hooks do Gestus..." -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host ""

# Verificar se está no diretório correto
if (-not (Test-Path "Gestus.sln")) {
    Write-Host "❌ Erro: Execute este script na raiz do projeto Gestus" -ForegroundColor Red
    Write-Host ""
    exit 1
}

# Verificar se Git está instalado
try {
    $null = git --version
} catch {
    Write-Host "❌ Erro: Git não está instalado ou não está no PATH" -ForegroundColor Red
    Write-Host ""
    exit 1
}

# Verificar se .NET está instalado
try {
    $DotnetVersion = dotnet --version
    Write-Host "✅ .NET SDK detectado: $DotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ Erro: .NET SDK não está instalado" -ForegroundColor Red
    Write-Host ""
    exit 1
}

# Criar diretório .git/hooks se não existir
$HooksDir = ".git\hooks"
if (-not (Test-Path $HooksDir)) {
    New-Item -ItemType Directory -Path $HooksDir -Force | Out-Null
}

# Copiar pre-commit hook
Write-Host "📝 Instalando pre-commit hook..." -ForegroundColor Cyan

$PreCommitSource = ".githooks\pre-commit.ps1"
$PreCommitDest = "$HooksDir\pre-commit"

if (-not (Test-Path $PreCommitSource)) {
    Write-Host "❌ Erro: Arquivo $PreCommitSource não encontrado" -ForegroundColor Red
    Write-Host ""
    exit 1
}

# Criar wrapper bash que chama o PowerShell
$BashWrapper = @"
#!/bin/sh
# Git Hook Pre-Commit - Gestus
# Chama o script PowerShell de validação

powershell.exe -ExecutionPolicy Bypass -File .githooks/pre-commit.ps1
exit `$?
"@

# Salvar wrapper
Set-Content -Path $PreCommitDest -Value $BashWrapper -NoNewline

Write-Host "✅ Pre-commit hook instalado" -ForegroundColor Green
Write-Host ""

# Configurar Git para usar hooks personalizados
Write-Host "📝 Configurando Git..." -ForegroundColor Cyan
git config core.hooksPath .git/hooks
Write-Host "✅ Git configurado" -ForegroundColor Green
Write-Host ""

# Testar se PowerShell está disponível
Write-Host "🧪 Testando configuração..." -ForegroundColor Cyan
try {
    $TestResult = powershell.exe -ExecutionPolicy Bypass -Command "Write-Output 'OK'"
    if ($TestResult -eq "OK") {
        Write-Host "✅ PowerShell funcionando corretamente" -ForegroundColor Green
    }
} catch {
    Write-Host "⚠️  Aviso: Não foi possível testar PowerShell" -ForegroundColor Yellow
}
Write-Host ""

# Resumo
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host ""
Write-Host "✅ Instalação concluída com sucesso!" -ForegroundColor Green
Write-Host ""
Write-Host "📋 Validações ativadas:" -ForegroundColor Cyan
Write-Host "  • Formatação de código" -ForegroundColor White
Write-Host "  • Build do projeto" -ForegroundColor White
Write-Host "  • Nomenclatura em português" -ForegroundColor White
Write-Host "  • Estrutura de arquivos" -ForegroundColor White
Write-Host "  • Segurança (logs e secrets)" -ForegroundColor White
Write-Host "  • Documentação XML" -ForegroundColor White
Write-Host ""
Write-Host "💡 Dica: Para desabilitar temporariamente, use:" -ForegroundColor Cyan
Write-Host "  git commit --no-verify" -ForegroundColor Gray
Write-Host ""
Write-Host "⚠️  Mas NÃO é recomendado! Os padrões existem por um motivo." -ForegroundColor Yellow
Write-Host ""
