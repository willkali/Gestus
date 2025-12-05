# ============================================
# Pre-Commit Hook - Validação de Padrões Gestus
# ============================================
# Este script valida TODOS os padrões antes de permitir commit
# Execução: Automática ao fazer 'git commit'
# Bypass: git commit --no-verify (NÃO RECOMENDADO!)

$ErrorActionPreference = "Stop"
$OriginalColor = $Host.UI.RawUI.ForegroundColor

# Cores para output
function Write-Success { param($Message) $Host.UI.RawUI.ForegroundColor = "Green"; Write-Host "✅ $Message"; $Host.UI.RawUI.ForegroundColor = $OriginalColor }
function Write-Error-Custom { param($Message) $Host.UI.RawUI.ForegroundColor = "Red"; Write-Host "❌ $Message"; $Host.UI.RawUI.ForegroundColor = $OriginalColor }
function Write-Warning-Custom { param($Message) $Host.UI.RawUI.ForegroundColor = "Yellow"; Write-Host "⚠️  $Message"; $Host.UI.RawUI.ForegroundColor = $OriginalColor }
function Write-Info { param($Message) $Host.UI.RawUI.ForegroundColor = "Cyan"; Write-Host "ℹ️  $Message"; $Host.UI.RawUI.ForegroundColor = $OriginalColor }

Write-Host ""
Write-Host "🔍 Validando padrões Gestus antes do commit..." -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host ""

$HasErrors = $false
$StartTime = Get-Date

# ============================================
# 1. Verificar se há arquivos staged
# ============================================
Write-Info "Verificando arquivos staged..."
$StagedFiles = git diff --cached --name-only --diff-filter=ACM
if ($StagedFiles.Count -eq 0) {
    Write-Warning-Custom "Nenhum arquivo staged para commit"
    exit 0
}

$CsFiles = $StagedFiles | Where-Object { $_ -like "*.cs" }
Write-Success "Encontrados $($StagedFiles.Count) arquivo(s) staged ($($CsFiles.Count) arquivos .cs)"
Write-Host ""

# ============================================
# 2. Validação de Formatação
# ============================================
Write-Info "Validando formatação do código..."
try {
    $FormatResult = dotnet format Gestus.sln --verify-no-changes --verbosity quiet 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Error-Custom "Código não está formatado corretamente!"
        Write-Host ""
        Write-Host "Execute o comando:" -ForegroundColor Yellow
        Write-Host "  dotnet format Gestus.sln" -ForegroundColor White
        Write-Host ""
        $HasErrors = $true
    } else {
        Write-Success "Formatação OK"
    }
} catch {
    Write-Error-Custom "Erro ao verificar formatação: $_"
    $HasErrors = $true
}
Write-Host ""

# ============================================
# 3. Build do Projeto
# ============================================
Write-Info "Compilando projeto..."
try {
    $BuildOutput = dotnet build Gestus.sln --configuration Release --no-restore --verbosity quiet 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Error-Custom "Build falhou!"
        Write-Host ""
        Write-Host "Erros de compilação:" -ForegroundColor Red
        Write-Host $BuildOutput -ForegroundColor Gray
        Write-Host ""
        $HasErrors = $true
    } else {
        Write-Success "Build OK"
    }
} catch {
    Write-Error-Custom "Erro ao compilar: $_"
    $HasErrors = $true
}
Write-Host ""

# ============================================
# 4. Validação de Nomenclatura
# ============================================
Write-Info "Validando nomenclatura em português..."

# Verificar classes em inglês (apenas arquivos staged)
$EnglishClasses = @()
foreach ($File in $CsFiles) {
    if (Test-Path $File) {
        $Content = Get-Content $File -Raw
        
        # Verificar classes/interfaces/enums em inglês comuns
        $EnglishPatterns = @(
            'public\s+class\s+User\b',
            'public\s+class\s+Role\b',
            'public\s+class\s+Permission\b',
            'public\s+class\s+Application\b',
            'public\s+class\s+Group\b',
            'public\s+interface\s+IUser',
            'public\s+interface\s+IRole',
            'public\s+interface\s+IPermission',
            'public\s+enum\s+UserStatus',
            'public\s+enum\s+RoleType'
        )
        
        foreach ($Pattern in $EnglishPatterns) {
            if ($Content -match $Pattern) {
                $EnglishClasses += "$File : $($Matches[0])"
            }
        }
    }
}

if ($EnglishClasses.Count -gt 0) {
    Write-Error-Custom "Encontradas classes/interfaces em inglês:"
    foreach ($Item in $EnglishClasses) {
        Write-Host "  $Item" -ForegroundColor Gray
    }
    Write-Host ""
    Write-Host "📋 Padrão: Classes devem estar em português" -ForegroundColor Yellow
    Write-Host "Exemplos: Usuario, Papel, Permissao, Aplicacao, Grupo" -ForegroundColor White
    Write-Host ""
    $HasErrors = $true
} else {
    Write-Success "Nomenclatura OK"
}

# Verificar métodos async sem sufixo Async
$MissingAsync = @()
foreach ($File in $CsFiles) {
    if (Test-Path $File) {
        $Lines = Get-Content $File
        for ($i = 0; $i -lt $Lines.Count; $i++) {
            if ($Lines[$i] -match 'public\s+async\s+Task.*\s+(\w+)\s*\(') {
                $MethodName = $Matches[1]
                if ($MethodName -notmatch 'Async$') {
                    $MissingAsync += "$File : Linha $($i+1) : $MethodName"
                }
            }
        }
    }
}

if ($MissingAsync.Count -gt 0) {
    Write-Error-Custom "Métodos async sem sufixo 'Async':"
    foreach ($Item in $MissingAsync) {
        Write-Host "  $Item" -ForegroundColor Gray
    }
    Write-Host ""
    Write-Host "📋 Padrão: Métodos async devem ter sufixo 'Async'" -ForegroundColor Yellow
    Write-Host ""
    $HasErrors = $true
} else {
    Write-Success "Sufixo Async OK"
}
Write-Host ""

# ============================================
# 5. Validação de Estrutura
# ============================================
Write-Info "Validando estrutura de arquivos..."

# Verificar múltiplas classes públicas no mesmo arquivo
$MultiClassFiles = @()
foreach ($File in $CsFiles) {
    if (Test-Path $File) {
        $Content = Get-Content $File -Raw
        $PublicClasses = ([regex]::Matches($Content, 'public\s+(class|interface|enum)\s+\w+')).Count
        
        if ($PublicClasses -gt 1) {
            $MultiClassFiles += "$File : $PublicClasses classes/interfaces/enums públicos"
        }
    }
}

if ($MultiClassFiles.Count -gt 0) {
    Write-Error-Custom "Arquivos com múltiplas classes públicas:"
    foreach ($Item in $MultiClassFiles) {
        Write-Host "  $Item" -ForegroundColor Gray
    }
    Write-Host ""
    Write-Host "📋 Padrão: Um arquivo = Uma classe/interface/enum" -ForegroundColor Yellow
    Write-Host ""
    $HasErrors = $true
} else {
    Write-Success "Estrutura de arquivos OK"
}

# Verificar DTOs em controllers
$DtosInControllers = @()
foreach ($File in $CsFiles) {
    if ($File -like "*Controllers*" -and (Test-Path $File)) {
        $Content = Get-Content $File -Raw
        if ($Content -match 'public\s+class\s+\w*(Request|Response|Dto)\b') {
            $DtosInControllers += $File
        }
    }
}

if ($DtosInControllers.Count -gt 0) {
    Write-Error-Custom "DTOs encontrados dentro de controllers:"
    foreach ($Item in $DtosInControllers) {
        Write-Host "  $Item" -ForegroundColor Gray
    }
    Write-Host ""
    Write-Host "📋 Padrão: DTOs devem estar em Gestus.Application/DTOs/" -ForegroundColor Yellow
    Write-Host ""
    $HasErrors = $true
} else {
    Write-Success "DTOs OK"
}
Write-Host ""

# ============================================
# 6. Validação de Segurança
# ============================================
Write-Info "Validando segurança..."

# Verificar logs de dados sensíveis
$SensitiveLogs = @()
foreach ($File in $CsFiles) {
    if (Test-Path $File) {
        $Lines = Get-Content $File
        for ($i = 0; $i -lt $Lines.Count; $i++) {
            if ($Lines[$i] -match 'Log(Information|Debug|Warning|Error).*\b(senha|password|token|secret)\b') {
                $SensitiveLogs += "$File : Linha $($i+1)"
            }
        }
    }
}

if ($SensitiveLogs.Count -gt 0) {
    Write-Warning-Custom "AVISO: Possível log de dados sensíveis:"
    foreach ($Item in $SensitiveLogs) {
        Write-Host "  $Item" -ForegroundColor Gray
    }
    Write-Host ""
    Write-Host "📋 Padrão: NUNCA logar senhas, tokens ou dados sensíveis" -ForegroundColor Yellow
    Write-Host "Revise manualmente se isso é realmente um problema" -ForegroundColor White
    Write-Host ""
    # Não bloqueia, apenas avisa
}

# Verificar secrets em appsettings
$SecretsInConfig = @()
$ConfigFiles = $StagedFiles | Where-Object { $_ -like "*appsettings*.json" -and $_ -notlike "*appsettings.json" }
foreach ($File in $ConfigFiles) {
    if (Test-Path $File) {
        $Content = Get-Content $File -Raw
        if ($Content -match '"(password|secret|key)"\s*:\s*"[^{]') {
            $SecretsInConfig += $File
        }
    }
}

if ($SecretsInConfig.Count -gt 0) {
    Write-Error-Custom "Secrets encontrados em arquivos de configuração:"
    foreach ($Item in $SecretsInConfig) {
        Write-Host "  $Item" -ForegroundColor Gray
    }
    Write-Host ""
    Write-Host "📋 Padrão: Usar variáveis de ambiente ou User Secrets" -ForegroundColor Yellow
    Write-Host "Exemplo: `"Password`": `"{ENV:DB_PASSWORD}`"" -ForegroundColor White
    Write-Host ""
    $HasErrors = $true
} else {
    Write-Success "Segurança OK"
}
Write-Host ""

# ============================================
# 7. Validação de Documentação
# ============================================
Write-Info "Validando documentação XML..."

$MissingDocs = @()
foreach ($File in $CsFiles) {
    if (Test-Path $File) {
        $Lines = Get-Content $File
        for ($i = 0; $i -lt $Lines.Count; $i++) {
            # Verificar classes públicas sem XML comment
            if ($Lines[$i] -match 'public\s+(class|interface)\s+\w+') {
                # Verificar se linha anterior tem ///
                if ($i -eq 0 -or $Lines[$i-1] -notmatch '///') {
                    $MissingDocs += "$File : Linha $($i+1)"
                }
            }
        }
    }
}

if ($MissingDocs.Count -gt 0) {
    Write-Warning-Custom "AVISO: Classes públicas sem XML comments:"
    foreach ($Item in $MissingDocs | Select-Object -First 5) {
        Write-Host "  $Item" -ForegroundColor Gray
    }
    if ($MissingDocs.Count -gt 5) {
        Write-Host "  ... e mais $($MissingDocs.Count - 5) arquivo(s)" -ForegroundColor Gray
    }
    Write-Host ""
    Write-Host "📋 Padrão: Todos os membros públicos devem ter XML comments" -ForegroundColor Yellow
    Write-Host "Isso não bloqueia o commit, mas deve ser corrigido" -ForegroundColor White
    Write-Host ""
    # Não bloqueia, apenas avisa
} else {
    Write-Success "Documentação OK"
}
Write-Host ""

# ============================================
# Resultado Final
# ============================================
$EndTime = Get-Date
$Duration = ($EndTime - $StartTime).TotalSeconds

Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
if ($HasErrors) {
    Write-Host ""
    Write-Error-Custom "COMMIT BLOQUEADO!"
    Write-Host ""
    Write-Host "Corrija os erros acima antes de commitar." -ForegroundColor Yellow
    Write-Host "Tempo de validação: $([math]::Round($Duration, 2))s" -ForegroundColor Gray
    Write-Host ""
    Write-Host "💡 Dica: Para ver todos os padrões, consulte PADRONIZACAO.md" -ForegroundColor Cyan
    Write-Host ""
    exit 1
} else {
    Write-Host ""
    Write-Success "TODAS AS VALIDAÇÕES PASSARAM!"
    Write-Host ""
    Write-Host "Tempo de validação: $([math]::Round($Duration, 2))s" -ForegroundColor Gray
    Write-Host "Prosseguindo com o commit..." -ForegroundColor Green
    Write-Host ""
    exit 0
}
