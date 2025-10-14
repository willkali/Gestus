# Script para executar testes do projeto Gestus
# Uso: .\test.ps1 [categoria] [filtro]
# Exemplos:
#   .\test.ps1                           # Todos os testes
#   .\test.ps1 unit                      # Testes unitários apenas
#   .\test.ps1 controller                # Testes de controllers apenas
#   .\test.ps1 auth                      # Testes de autenticação apenas
#   .\test.ps1 custom "MinhaClasse"      # Filtro personalizado

param(
    [string]$Category = "",
    [string]$Filter = ""
)

# Caminho do projeto de testes
$TestProject = "Tests\Gestus.Tests\Gestus.Tests.csproj"

# Função para executar testes com filtro
function Invoke-TestsWithFilter([string]$FilterExpression, [string]$Description) {
    Write-Host "🎯 $Description" -ForegroundColor Cyan
    dotnet test $TestProject --filter $FilterExpression --verbosity normal
}

# Escolher filtro baseado na categoria
switch ($Category.ToLower()) {
    "unit" {
        Invoke-TestsWithFilter "Category!=Controller" "Executando testes unitários (exceto controllers)"
    }
    "controller" {
        Invoke-TestsWithFilter "Category~Controller" "Executando todos os testes de controllers"
    }
    "auth" {
        Invoke-TestsWithFilter "Category=Controller.Autenticacao" "Executando testes do AutenticacaoController"
    }
    "login" {
        Invoke-TestsWithFilter "Category=Controller.Autenticacao&Category=Action.Login" "Executando testes de Login"
    }
    "custom" {
        if ($Filter) {
            Invoke-TestsWithFilter "DisplayName~$Filter" "Executando testes: $Filter"
        } else {
            Write-Host "❌ Para categoria 'custom', forneça um filtro: .\test.ps1 custom 'MeuFiltro'" -ForegroundColor Red
            exit 1
        }
    }
    "help" {
        Write-Host "📚 Ajuda - Como usar o script test.ps1" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Uso: .\test.ps1 [categoria] [filtro]" -ForegroundColor White
        Write-Host ""
        Write-Host "Categorias disponíveis:" -ForegroundColor Cyan
        Write-Host "  • (vazio)     - Executa todos os testes" -ForegroundColor Gray
        Write-Host "  • unit        - Testes unitários (exceto controllers)" -ForegroundColor Gray
        Write-Host "  • controller  - Todos os testes de controllers" -ForegroundColor Gray
        Write-Host "  • auth        - Testes do AutenticacaoController" -ForegroundColor Gray
        Write-Host "  • login       - Testes específicos de Login" -ForegroundColor Gray
        Write-Host "  • custom      - Filtro personalizado (requer parâmetro Filter)" -ForegroundColor Gray
        Write-Host "  • help        - Mostra esta ajuda" -ForegroundColor Gray
        Write-Host ""
        Write-Host "Exemplos:" -ForegroundColor Cyan
        Write-Host "  .\test.ps1                    # Todos os testes" -ForegroundColor Green
        Write-Host "  .\test.ps1 auth               # Testes de autenticação" -ForegroundColor Green
        Write-Host "  .\test.ps1 custom 'Usuario'   # Testes que contenham 'Usuario'" -ForegroundColor Green
        Write-Host ""
        exit 0
    }
    default {
        Write-Host "🧪 Executando todos os testes" -ForegroundColor Cyan
        dotnet test $TestProject --verbosity normal
    }
}

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Testes concluídos com sucesso!" -ForegroundColor Green
} else {
    Write-Host "❌ Alguns testes falharam!" -ForegroundColor Red
}
