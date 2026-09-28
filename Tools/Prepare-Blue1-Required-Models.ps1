param(
    [string]$SourceRoot = "C:\Users\roxy\OneDrive\Masaüstü\Metin2BE-Client-master",
    [string]$MapRoot = "",
    [string]$ExportRoot = "",
    [switch]$CopyOnly
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($MapRoot)) {
    $MapRoot = Join-Path $SourceRoot "metin2_map_empire\metin2_empire_blue_1"
}
if ([string]::IsNullOrWhiteSpace($ExportRoot)) {
    $ExportRoot = Join-Path $SourceRoot "_BLUE1_REQUIRED_GR2"
}

if (-not (Test-Path -LiteralPath $MapRoot)) { throw "Blue 1 bulunamadı: $MapRoot" }
$propertyRoot = Join-Path $SourceRoot "property"
if (-not (Test-Path -LiteralPath $propertyRoot)) { throw "property klasörü bulunamadı: $propertyRoot" }

New-Item -ItemType Directory -Force -Path $ExportRoot | Out-Null

function Normalize-Key([string]$value) {
    $name = [IO.Path]::GetFileNameWithoutExtension($value)
    $lod = $name.IndexOf("_lod_", [StringComparison]::OrdinalIgnoreCase)
    if ($lod -ge 0) { $name = $name.Substring(0, $lod) }
    return $name.Replace(" ", "_").Trim().ToLowerInvariant()
}

# 1) Blue 1 AreaData'daki property ID -> buildingfile model adlarını çıkar.
$properties = @{}
Get-ChildItem -LiteralPath $propertyRoot -Recurse -File -Filter "*.prb" | ForEach-Object {
    $lines = Get-Content -LiteralPath $_.FullName
    if ($lines.Count -lt 3) { return }
    [uint32]$id = 0
    if (-not [uint32]::TryParse($lines[1].Trim(), [ref]$id)) { return }

    $model = $null
    foreach ($line in $lines) {
        if ($line.Trim() -match '^buildingfile\s+"([^"]+)"') {
            $model = $Matches[1]
            break
        }
    }
    if ($model) { $properties[$id] = $model }
}

# 2) Blue 1 AreaData'daki tüm property ID'leri topla.
$ids = New-Object 'System.Collections.Generic.HashSet[uint32]'
$areaFiles = Get-ChildItem -LiteralPath $MapRoot -Recurse -File -Filter "areadata.txt"
foreach ($area in $areaFiles) {
    $lines = Get-Content -LiteralPath $area.FullName
    for ($i=0; $i -lt $lines.Count; $i++) {
        if ($lines[$i].TrimStart() -like "Start Object*") {
            if ($i + 2 -lt $lines.Count) {
                [uint32]$id = 0
                if ([uint32]::TryParse($lines[$i+2].Trim(), [ref]$id)) { [void]$ids.Add($id) }
            }
        }
    }
}

# 3) Sadece gerçekten Blue 1 tarafından kullanılan buildingfile'ları seç.
$models = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
$unresolved = 0
foreach ($id in $ids) {
    if ($properties.ContainsKey($id)) {
        [void]$models.Add($properties[$id])
    } else {
        $unresolved++
    }
}

# 4) Kaynak client içindeki GR2'leri basename üzerinden eşleştir.
$gr2Index = @{}
Get-ChildItem -LiteralPath $SourceRoot -Recurse -File -Filter "*.gr2" |
    Where-Object { $_.FullName -notmatch "\\.git(\\|$)" -and $_.FullName -notmatch "\\_BLUE1_REQUIRED_GR2(\\|$)" } |
    ForEach-Object {
        $key = Normalize-Key $_.Name
        if (-not $gr2Index.ContainsKey($key)) { $gr2Index[$key] = $_.FullName }
    }

$found = 0
$missing = 0
$commands = New-Object System.Collections.Generic.List[string]
$manifest = New-Object System.Collections.Generic.List[string]

foreach ($model in $models) {
    $key = Normalize-Key $model
    if (-not $gr2Index.ContainsKey($key)) {
        $missing++
        continue
    }

    $source = $gr2Index[$key]
    $relative = $source.Substring($SourceRoot.Length).TrimStart('')
    $dest = Join-Path $ExportRoot $relative
    New-Item -ItemType Directory -Force -Path (Split-Path $dest -Parent) | Out-Null

    $out = [IO.Path]::ChangeExtension($dest, ".fbx")
    $commands.Add(('"{0}" "{1}"' -f $source, $out))
    $manifest.Add(("{0}|{1}|{2}" -f $model, $source, $out))
    $found++

    if ($CopyOnly) { Copy-Item -LiteralPath $source -Destination $dest -Force }
}

$commandsPath = Join-Path $ExportRoot "Blue1-Noesis-Commands.txt"
$manifestPath = Join-Path $ExportRoot "Blue1-Required-Models.txt"
$commands | Set-Content -LiteralPath $commandsPath -Encoding UTF8
$manifest | Set-Content -LiteralPath $manifestPath -Encoding UTF8

Write-Host ""
Write-Host "BLUE 1 GEREKEN BINA MODELLERI HAZIRLAYICI" -ForegroundColor Cyan
Write-Host "AreaData property ID : $($ids.Count)"
Write-Host "Building model       : $($models.Count)"
Write-Host "GR2 bulundu           : $found" -ForegroundColor Green
Write-Host "GR2 bulunamadi        : $missing" -ForegroundColor Yellow
Write-Host "Property cozulemedi   : $unresolved" -ForegroundColor Yellow
Write-Host "Noesis komut listesi  : $commandsPath"
Write-Host "Manifest              : $manifestPath"
Write-Host ""
Write-Host "Noesis -> Tools -> Batch Process ile Blue1-Noesis-Commands.txt kullan."
