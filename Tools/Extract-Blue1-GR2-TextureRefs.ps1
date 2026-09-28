param(
    [string]$SourceRoot = "",
    [string]$Manifest = "",
    [string]$OutputRoot = "C:\M2Blue1_Noesis\GR2TextureRefs"
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($SourceRoot)) {
    $desktop = [Environment]::GetFolderPath("Desktop")
    $SourceRoot = Join-Path $desktop "Metin2BE-Client-master"
}
if ([string]::IsNullOrWhiteSpace($Manifest)) {
    $Manifest = Join-Path $SourceRoot "_BLUE1_REQUIRED_GR2\Blue1-Required-Models.txt"
}
if (-not (Test-Path -LiteralPath $SourceRoot)) { throw "SourceRoot bulunamadi: $SourceRoot" }
if (-not (Test-Path -LiteralPath $Manifest)) { throw "Manifest bulunamadi: $Manifest" }

New-Item -ItemType Directory -Force -Path $OutputRoot | Out-Null

function Normalize([string]$v) {
    if ([string]::IsNullOrWhiteSpace($v)) { return "" }
    $v = [IO.Path]::GetFileNameWithoutExtension($v)
    return ($v -replace '[^a-zA-Z0-9]', '').ToLowerInvariant()
}

function Read-Gr2TextureNames([string]$path) {
    $bytes = [IO.File]::ReadAllBytes($path)
    $found = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)

    $ascii = [Text.Encoding]::ASCII.GetString($bytes)
    foreach ($m in [regex]::Matches($ascii, '(?i)([a-z0-9_ .()/\\-]+?\.(?:dds|tga|png))')) {
        $name = $m.Groups[1].Value.Trim()
        if ($name.Length -gt 2) { [void]$found.Add($name) }
    }

    if ($bytes.Length -ge 4) {
        $unicode = [Text.Encoding]::Unicode.GetString($bytes)
        foreach ($m in [regex]::Matches($unicode, '(?i)([a-z0-9_ .()/\\-]+?\.(?:dds|tga|png))')) {
            $name = $m.Groups[1].Value.Trim()
            if ($name.Length -gt 2) { [void]$found.Add($name) }
        }
    }
    return @($found)
}

$lines = Get-Content -LiteralPath $Manifest
$results = New-Object System.Collections.Generic.List[object]
$gr2Count = 0
$refCount = 0
$tab = [char]9

foreach ($line in $lines) {
    if (-not $line.StartsWith("FOUND")) { continue }
    $parts = $line -split $tab
    if ($parts.Count -lt 4) { continue }

    $model = $parts[1]
    $relative = $parts[2]
    $gr2 = Join-Path $SourceRoot $relative
    if (-not (Test-Path -LiteralPath $gr2)) { continue }

    $gr2Count++
    $refs = @(Read-Gr2TextureNames $gr2)
    foreach ($ref in $refs) {
        $results.Add([pscustomobject]@{
            Model = $model
            GR2 = $relative
            TextureRef = $ref
            TextureKey = Normalize $ref
        })
        $refCount++
    }
}

$outFile = Join-Path $OutputRoot "Blue1-GR2-TextureRefs.csv"
$results | Export-Csv -LiteralPath $outFile -NoTypeInformation -Encoding UTF8

Write-Host ""
Write-Host "BLUE 1 GR2 TEXTURE REFERANSLARI" -ForegroundColor Cyan
Write-Host "GR2 tarandi : $gr2Count"
Write-Host "Texture ref : $refCount"
Write-Host "CSV         : $outFile"
Write-Host ""
Write-Host "TAMAMLANDI" -ForegroundColor Green
