param(
    [string]$SourceRoot = "",
    [string]$MapRoot = "",
    [string]$ExportRoot = "",
    [switch]$CopyOnly
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($SourceRoot)) {
    $desktop = [Environment]::GetFolderPath("Desktop")
    $SourceRoot = Join-Path $desktop "Metin2BE-Client-master"
}

if ([string]::IsNullOrWhiteSpace($MapRoot)) {
    $MapRoot = Join-Path $SourceRoot "metin2_map_empire\metin2_empire_blue_1"
}
if ([string]::IsNullOrWhiteSpace($ExportRoot)) {
    $ExportRoot = Join-Path $SourceRoot "_BLUE1_REQUIRED_GR2"
}

Write-Host ""
Write-Host "BLUE 1 GEREKEN BINA MODELLERI HAZIRLAYICI" -ForegroundColor Cyan
Write-Host "SourceRoot: $SourceRoot"
Write-Host "MapRoot: $MapRoot"
Write-Host "ExportRoot: $ExportRoot"
Write-Host ""

if (-not (Test-Path -LiteralPath $SourceRoot)) { throw "SourceRoot bulunamadi: $SourceRoot" }
if (-not (Test-Path -LiteralPath $MapRoot)) { throw "MapRoot bulunamadi: $MapRoot" }

function Normalize-Key([string]$Value) {
    if ([string]::IsNullOrWhiteSpace($Value)) { return "" }
    $v = $Value.Replace("\", "/")
    $v = [System.IO.Path]::GetFileNameWithoutExtension($v)
    return ($v -replace '[^a-zA-Z0-9]', '').ToLowerInvariant()
}

function Extract-QuotedValue([string[]]$Lines, [string]$Key) {
    $pattern = '^\s*' + [regex]::Escape($Key) + '\s+"([^"]+)"'
    foreach ($line in $Lines) {
        if ($line -match $pattern) { return $matches[1] }
    }
    return $null
}

$propertyRoot = Join-Path $SourceRoot "property"
$properties = @{}
$propertyFiles = @(Get-ChildItem -LiteralPath $propertyRoot -Filter "*.prb" -Recurse -File -ErrorAction SilentlyContinue)

foreach ($file in $propertyFiles) {
    $lines = Get-Content -LiteralPath $file.FullName
    if ($lines.Count -lt 3) { continue }
    [uint32]$id = 0
    if (-not [uint32]::TryParse($lines[1].Trim(), [ref]$id)) { continue }
    $model = Extract-QuotedValue $lines "buildingfile"
    if ([string]::IsNullOrWhiteSpace($model)) { continue }
    $properties[$id] = $model
}

$usedIds = New-Object 'System.Collections.Generic.HashSet[uint32]'
$areaFiles = @(Get-ChildItem -LiteralPath $MapRoot -Filter "areadata.txt" -Recurse -File)

foreach ($file in $areaFiles) {
    $lines = Get-Content -LiteralPath $file.FullName
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i].Trim() -like "Start Object*") {
            if ($i + 2 -lt $lines.Count) {
                [uint32]$id = 0
                if ([uint32]::TryParse($lines[$i + 2].Trim(), [ref]$id)) {
                    [void]$usedIds.Add($id)
                }
            }
        }
    }
}

$requiredModels = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
$unresolvedProperties = 0

foreach ($id in $usedIds) {
    if ($properties.ContainsKey($id)) {
        [void]$requiredModels.Add($properties[$id])
    } else {
        $unresolvedProperties++
    }
}

$gr2Index = @{}
$gr2Files = @(Get-ChildItem -LiteralPath $SourceRoot -Filter "*.gr2" -Recurse -File)
foreach ($file in $gr2Files) {
    $key = Normalize-Key $file.Name
    if (-not $gr2Index.ContainsKey($key)) {
        $gr2Index[$key] = $file.FullName
    }
}

New-Item -ItemType Directory -Force -Path $ExportRoot | Out-Null
$commandsPath = Join-Path $ExportRoot "Blue1-Noesis-Commands.txt"
$manifestPath = Join-Path $ExportRoot "Blue1-Required-Models.txt"

$found = 0
$missing = 0
$commandLines = New-Object System.Collections.Generic.List[string]
$manifestLines = New-Object System.Collections.Generic.List[string]
$tab = [char]9

foreach ($model in ($requiredModels | Sort-Object)) {
    $key = Normalize-Key $model
    $source = $null
    if ($gr2Index.ContainsKey($key)) { $source = $gr2Index[$key] }

    if ($source) {
        $found++
        $relative = $source.Substring($SourceRoot.Length).TrimStart('\')
        $safeName = [System.IO.Path]::GetFileNameWithoutExtension($source)
        $out = Join-Path $ExportRoot ("FBX\" + $safeName + ".fbx")
        New-Item -ItemType Directory -Force -Path (Split-Path $out -Parent) | Out-Null
        $commandLines.Add(('"{0}" "{1}"' -f $source, $out))
        $manifestLines.Add(("FOUND{0}{1}{0}{2}{0}{3}" -f $tab, $model, $relative, $out))
    } else {
        $missing++
        $manifestLines.Add(("MISSING{0}{1}" -f $tab, $model))
    }
}

$commandLines | Set-Content -LiteralPath $commandsPath -Encoding UTF8
$manifestLines | Set-Content -LiteralPath $manifestPath -Encoding UTF8

Write-Host "AreaData property ID : $($usedIds.Count)"
Write-Host "Building model       : $($requiredModels.Count)"
Write-Host "GR2 bulundu           : $found"
Write-Host "GR2 bulunamadi        : $missing"
Write-Host "Property cozulemedi   : $unresolvedProperties"
Write-Host "Noesis komut listesi  : $commandsPath"
Write-Host "Manifest              : $manifestPath"
Write-Host ""
Write-Host "TAMAMLANDI" -ForegroundColor Green
