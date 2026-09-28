param(
    [string]$SourceRoot = "",
    [string]$FbxRoot = "C:\M2Blue1_Noesis\FBX",
    [string]$UnityTextureRoot = "",
    [switch]$ConvertNow
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($SourceRoot)) {
    $desktop = [Environment]::GetFolderPath("Desktop")
    $SourceRoot = Join-Path $desktop "Metin2BE-Client-master"
}
if ([string]::IsNullOrWhiteSpace($UnityTextureRoot)) {
    $desktop = [Environment]::GetFolderPath("Desktop")
    $UnityTextureRoot = Join-Path $desktop "Metin2-REBORN-Git\Metin2Mobile\Assets\Metin2\Imported\Blue1Required\Textures"
}

if (-not (Test-Path -LiteralPath $SourceRoot)) { throw "SourceRoot bulunamadi: $SourceRoot" }
if (-not (Test-Path -LiteralPath $FbxRoot)) { throw "FBX klasoru bulunamadi: $FbxRoot" }

New-Item -ItemType Directory -Force -Path $UnityTextureRoot | Out-Null

function Normalize-Key([string]$Value) {
    if ([string]::IsNullOrWhiteSpace($Value)) { return "" }
    $v = [System.IO.Path]::GetFileNameWithoutExtension($Value)
    return ($v -replace '[^a-zA-Z0-9]', '').ToLowerInvariant()
}

Write-Host ""
Write-Host "BLUE 1 GEREKEN TEXTURE HAZIRLAYICI" -ForegroundColor Cyan
Write-Host "FBX: $FbxRoot"
Write-Host "Texture hedefi: $UnityTextureRoot"
Write-Host ""

# FBX exports normally retain texture file names in their material definitions.
$required = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
$fbxFiles = @(Get-ChildItem -LiteralPath $FbxRoot -Filter "*.fbx" -Recurse -File)

foreach ($fbx in $fbxFiles) {
    try {
        $raw = [System.IO.File]::ReadAllText($fbx.FullName)
        $matches = [regex]::Matches($raw, '(?i)(?:[^"''\\/\r\n]*[\\/])?([^"''\\/\r\n]+\.dds)')
        foreach ($m in $matches) {
            $name = $m.Groups[1].Value
            if (-not [string]::IsNullOrWhiteSpace($name)) { [void]$required.Add($name) }
        }
    } catch {
        Write-Warning "FBX okunamadi: $($fbx.FullName)"
    }
}

Write-Host "FBX sayisi: $($fbxFiles.Count)"
Write-Host "FBX icinden bulunan DDS adi: $($required.Count)"

# Index all source DDS files once, by normalized basename.
$ddsIndex = @{}
$ddsFiles = @(Get-ChildItem -LiteralPath $SourceRoot -Filter "*.dds" -Recurse -File)
foreach ($dds in $ddsFiles) {
    $key = Normalize-Key $dds.Name
    if (-not $ddsIndex.ContainsKey($key)) {
        $ddsIndex[$key] = $dds.FullName
    }
}

$found = 0
$missing = 0
$copied = New-Object System.Collections.Generic.List[string]

foreach ($name in ($required | Sort-Object)) {
    $key = Normalize-Key $name
    if (-not $ddsIndex.ContainsKey($key)) {
        $missing++
        Write-Warning "DDS bulunamadi: $name"
        continue
    }

    $source = $ddsIndex[$key]
    $dest = Join-Path $UnityTextureRoot ([System.IO.Path]::GetFileNameWithoutExtension($source) + ".dds")
    Copy-Item -LiteralPath $source -Destination $dest -Force
    $copied.Add($dest)
    $found++
}

$manifest = Join-Path (Split-Path $UnityTextureRoot -Parent) "Blue1-Required-Textures.txt"
$copied | Set-Content -LiteralPath $manifest -Encoding UTF8

Write-Host ""
Write-Host "DDS bulundu : $found" -ForegroundColor Green
Write-Host "DDS eksik   : $missing" -ForegroundColor Yellow
Write-Host "Manifest    : $manifest"

if ($ConvertNow -and $copied.Count -gt 0) {
    $desktop = [Environment]::GetFolderPath("Desktop")
    $noesis = Join-Path $desktop "shared_3d_exporting\noesis\noesis\Noesis.exe"
    if (-not (Test-Path -LiteralPath $noesis)) { throw "Noesis.exe bulunamadi: $noesis" }

    $tempRoot = "C:\M2Blue1_Noesis\Textures"
    $tempIn = Join-Path $tempRoot "DDS"
    $tempOut = Join-Path $tempRoot "PNG"
    Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Force -Path $tempIn,$tempOut | Out-Null

    $done = 0
    $failed = 0

    foreach ($dds in $copied) {
        $name = [System.IO.Path]::GetFileNameWithoutExtension($dds)
        $asciiIn = Join-Path $tempIn ($name + ".dds")
        $asciiOut = Join-Path $tempOut ($name + ".png")
        Copy-Item -LiteralPath $dds -Destination $asciiIn -Force

        & $noesis "?cmode" $asciiIn $asciiOut 2>&1 | Out-Null

        if (Test-Path -LiteralPath $asciiOut) {
            Copy-Item -LiteralPath $asciiOut -Destination (Join-Path $UnityTextureRoot ($name + ".png")) -Force
            $done++
        } else {
            $failed++
            Write-Warning "Texture donusmedi: $name.dds"
        }
    }

    Write-Host "PNG basarili: $done" -ForegroundColor Green
    Write-Host "PNG hatali  : $failed" -ForegroundColor Yellow

    # Unity should use PNGs. Remove DDS copies so the problematic DDS importer
    # cannot override or conflict with the generated PNG assets.
    foreach ($dds in $copied) {
        Remove-Item -LiteralPath $dds -Force -ErrorAction SilentlyContinue
    }
}

Write-Host ""
Write-Host "TAMAMLANDI" -ForegroundColor Green
