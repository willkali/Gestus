# Script para executar testes do Gestus IAM
# Uso: .\tests\scripts\run-tests.ps1 [opções]

param(
    [switch]$Coverage,      # Gerar relatório de cobertura
    [switch]$Verbose,       # Output detalhado
    [string]$Filter = "",   # Filtrar testes específicos
    [switch]$Watch,         # Modo watch para desenvolvimento
    [switch]$Quick,         # Apenas testes unitários (mais rápido)
    [switch]$Help           # Mostrar ajuda
)

# Função para mostrar ajuda
function Show-Help {
    Write-Host @"
🧪 Script de Testes - Gestus IAM
================================

USO:
    .\tests\scripts\run-tests.ps1 [opções]

OPÇÕES:
    -Coverage       Gera relatório de cobertura de código
    -Verbose        Mostra output detalhado dos testes
    -Filter <nome>  Executa apenas testes que contenham o nome especificado
    -Watch          Executa testes em modo watch (re-executa quando arquivos mudam)
    -Quick          Executa apenas testes unitários (mais rápido)
    -Help           Mostra esta ajuda

EXEMPLOS:
    .\tests\scripts\run-tests.ps1                    # Executa todos os testes
    .\tests\scripts\run-tests.ps1 -Coverage          # Executa com cobertura
    .\tests\scripts\run-tests.ps1 -Filter "Usuario"  # Executa apenas testes de Usuario
    .\tests\scripts\run-tests.ps1 -Quick -Watch      # Modo desenvolvimento rápido
    .\tests\scripts\run-tests.ps1 -Verbose           # Output detalhado

"@ -ForegroundColor Cyan
    exit 0
}

if ($Help) { Show-Help }

# Configurações
$ErrorActionPreference = "Stop"
$testProject = "tests\Gestus.Tests\Gestus.Tests.csproj"
$resultsPath = "tests\results"

# Verificar se estamos no diretório correto
if (!(Test-Path "Gestus.csproj")) {
    Write-Host "❌ Execute este script a partir do diretório raiz do projeto Gestus" -ForegroundColor Red
    exit 1
}

# Criar diretório de resultados
if (!(Test-Path $resultsPath)) {
    New-Item -ItemType Directory -Path $resultsPath | Out-Null
}

try {
    Write-Host "🧪 Executando Testes - Gestus IAM" -ForegroundColor Cyan
    Write-Host "=================================" -ForegroundColor Cyan
    Write-Host "⏱ $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
    Write-Host ""

    # Construir argumentos do comando
    $testArgs = @("test", $testProject)
    
    if ($Coverage) {
        Write-Host "📊 Cobertura de código habilitada" -ForegroundColor Yellow
        $testArgs += @("--collect", "XPlat Code Coverage")
        $testArgs += @("--results-directory", $resultsPath)
    }
    
    if ($Verbose) {
        $testArgs += @("--verbosity", "detailed")
    } else {
        $testArgs += @("--verbosity", "minimal")
    }
    
    if ($Filter) {
        Write-Host "🎯 Filtrando por: $Filter" -ForegroundColor Yellow
        $testArgs += @("--filter", "DisplayName~$Filter")
    }
    
    if ($Watch) {
        Write-Host "👁 Modo Watch ativo - Pressione Ctrl+C para parar" -ForegroundColor Green
        $testArgs += "--watch"
    }

    # Log da configuração
    if ($Quick) {
        Write-Host "⚡ Modo rápido: apenas testes unitários" -ForegroundColor Green
    }
    
    Write-Host "🔧 Comando: dotnet $($testArgs -join ' ')" -ForegroundColor Gray
    Write-Host ""

    # Executar testes
    $startTime = Get-Date
    & dotnet @testArgs
    $endTime = Get-Date
    $duration = $endTime - $startTime
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "✅ Todos os testes passaram!" -ForegroundColor Green
        Write-Host "⏱ Tempo total: $($duration.TotalSeconds.ToString('F2')) segundos" -ForegroundColor Gray
        
        if ($Coverage) {
            Write-Host ""
            Write-Host "📈 Relatórios de cobertura:" -ForegroundColor Cyan
            Get-ChildItem $resultsPath -Recurse -Filter "coverage.cobertura.xml" | ForEach-Object {
                Write-Host "   $($_.FullName)" -ForegroundColor Gray
            }
        }
        
    } else {
        Write-Host ""
        Write-Host "❌ Alguns testes falharam!" -ForegroundColor Red
        Write-Host "⏱ Tempo total: $($duration.TotalSeconds.ToString('F2')) segundos" -ForegroundColor Gray
        exit 1
    }
    
} catch {
    Write-Host ""
    Write-Host "💥 Erro durante execução dos testes:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "🏁 Execução concluída!" -ForegroundColor Green