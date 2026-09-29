# Atualiza o Aldaria com a versao do GitHub e abre no Unity (pela linha de comando) ja no Play.
#
# Primeira vez (PowerShell, em qualquer pasta):
#   irm https://raw.githubusercontent.com/Niclad1/aldaria/main/atualizar.ps1 | iex
# Depois: clique duas vezes em atualizar.bat dentro da pasta do projeto.
#
# Descobre a pasta do projeto sozinho (lista de projetos do Unity Hub). Para forcar outra:
#   $env:ALDARIA_PATH = "C:\caminho\aldaria"
$ErrorActionPreference = "Stop"
$Versao = "6000.0.84f1"
$Zip = "https://github.com/Niclad1/aldaria/archive/refs/heads/main.zip"

function Is-Project($p) { $p -and (Test-Path (Join-Path $p "ProjectSettings")) -and (Test-Path (Join-Path $p "Assets")) }

# 1. Onde esta o projeto?
$proj = $null
if (Is-Project $env:ALDARIA_PATH) { $proj = $env:ALDARIA_PATH }
elseif ($PSScriptRoot -and (Is-Project $PSScriptRoot)) { $proj = $PSScriptRoot }
elseif (Is-Project (Get-Location).Path) { $proj = (Get-Location).Path }
else {
    $hub = Join-Path $env:APPDATA "UnityHub\projects-v1.json"
    if (Test-Path $hub) {
        $json = Get-Content $hub -Raw | ConvertFrom-Json
        foreach ($p in $json.data.PSObject.Properties) {
            $path = $p.Value.path
            if ($path -match "aldaria" -and (Is-Project $path)) { $proj = $path; break }
        }
    }
}
if (-not $proj) {
    $proj = Join-Path ([Environment]::GetFolderPath("MyDocuments")) "aldaria"
    Write-Host "Projeto nao encontrado no Unity Hub; vou criar em $proj"
    New-Item -ItemType Directory -Force $proj | Out-Null
}
Write-Host "Projeto: $proj"

# 2. O Unity precisa estar fechado para trocar os arquivos.
$aberto = Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" -ErrorAction SilentlyContinue |
    Where-Object { $_.CommandLine -and $_.CommandLine -match [regex]::Escape($proj) }
if ($aberto) {
    Write-Host "O Unity esta aberto com o projeto. Fechando..."
    $aberto | ForEach-Object { Stop-Process -Id $_.ProcessId -Force }
    Start-Sleep -Seconds 3
}

# 3. Atualiza: git pull se for um clone; senao baixa o zip e espelha as pastas.
if ((Test-Path (Join-Path $proj ".git")) -and (Get-Command git -ErrorAction SilentlyContinue)) {
    git -C $proj pull --ff-only
} else {
    $tmp = Join-Path $env:TEMP ("aldaria-" + [guid]::NewGuid())
    New-Item -ItemType Directory $tmp | Out-Null
    Write-Host "Baixando a versao nova..."
    Invoke-WebRequest $Zip -OutFile "$tmp\main.zip" -UseBasicParsing
    Expand-Archive "$tmp\main.zip" $tmp
    $src = Join-Path $tmp "aldaria-main"
    foreach ($d in "Assets", "Packages", "ProjectSettings", "tools", "art", "tests", ".github") {
        if (Test-Path "$src\$d") {
            # /MIR apaga o que saiu do projeto; /XF *.meta preserva os .meta que a Unity gerou ai.
            robocopy "$src\$d" "$proj\$d" /MIR /XF *.meta /NFL /NDL /NJH /NJS /NP | Out-Null
            if ($LASTEXITCODE -ge 8) { throw "Falha copiando $d" }
        }
    }
    Get-ChildItem $src -File | Copy-Item -Destination $proj -Force
    Remove-Item $tmp -Recurse -Force
}
$global:LASTEXITCODE = 0
Write-Host "Projeto atualizado."

# 4. Acha o Unity (instalado pelo Hub) e abre ja no Play.
$bases = @("$env:ProgramFiles\Unity\Hub\Editor", "${env:ProgramFiles(x86)}\Unity\Hub\Editor")
$hubCfg = Join-Path $env:APPDATA "UnityHub\secondaryInstallPath.json"
if (Test-Path $hubCfg) { $extra = (Get-Content $hubCfg -Raw | ConvertFrom-Json); if ($extra) { $bases += $extra } }
$unity = $null
foreach ($b in $bases) {
    $exato = Join-Path $b "$Versao\Editor\Unity.exe"
    if (Test-Path $exato) { $unity = $exato; break }
}
if (-not $unity) {
    foreach ($b in $bases) {
        if (-not (Test-Path $b)) { continue }
        $v = Get-ChildItem $b -Directory | Where-Object Name -like "6000.*" | Sort-Object Name -Descending | Select-Object -First 1
        if ($v -and (Test-Path "$($v.FullName)\Editor\Unity.exe")) { $unity = "$($v.FullName)\Editor\Unity.exe"; break }
    }
}
if (-not $unity) { throw "Nao achei o Unity 6 instalado pelo Hub. Instale a versao $Versao no Unity Hub." }

Write-Host "Abrindo com $unity"
Start-Process $unity -ArgumentList @("-projectPath", "`"$proj`"", "-executeMethod", "Aldaria.EditorTools.Builder.Play")
