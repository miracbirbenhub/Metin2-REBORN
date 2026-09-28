param(
    [string]$SourceRoot = "",
    [string]$Manifest = "",
    [string]$UnityRoot = ""
)

$ErrorActionPreference = "Stop"
$desktop = [Environment]::GetFolderPath("Desktop")
if ([string]::IsNullOrWhiteSpace($SourceRoot)) { $SourceRoot = Join-Path $desktop "Metin2BE-Client-master" }
if ([string]::IsNullOrWhiteSpace($Manifest)) { $Manifest = Join-Path $SourceRoot " _BLUE1_REQUIRED_GR2\Blue1-Required-Models.txt" }
if ([string]::IsNullOrWhiteSpace($UnityRoot)) { $UnityRoot = Join-Path $desktop "Metin2-REBORN-Git\Metin2Mobile\Assets\Metin2\Imported\Blue1Required" }
$Manifest = $Manifest.Trim()
if (-not (Test-Path -LiteralPath $Manifest)) { throw "Manifest bulunamadi: $Manifest" }
$textureRoot = Join-Path $UnityRoot "Textures"
New-Item -ItemType Directory -Force -Path $textureRoot | Out-Null
Write-Host "BLUE 1 TEXTURE FAST PREP" -ForegroundColor Cyan
Write-Host "Manifest: $Manifest"
$ddsIndex = @{}
Get-ChildItem -LiteralPath $SourceRoot -Filter "*.dds" -Recurse -File | ForEach-Object {
    $key = $_.Name.ToLowerInvariant()
    if (-not $ddsIndex.ContainsKey($key)) { $ddsIndex[$key] = $_.FullName }
}
Write-Host "Kaynak DDS indexi: $($ddsIndex.Count)"
$sourceDirs = New-Object "System.Collections.Generic.HashSet[string]" ([System.StringComparer]::OrdinalIgnoreCase)
foreach ($line in Get-Content -LiteralPath $Manifest) {
    if ($line -notmatch "^FOUND\t") { continue }
    $parts = $line -split ([char]9)
    if ($parts.Count -ge 3) {
        $full = Join-Path $SourceRoot $parts[2]
        if (Test-Path -LiteralPath $full) { [void]$sourceDirs.Add((Split-Path $full -Parent)) }
    }
}
$copied = @{}
foreach ($dir in $sourceDirs) {
    $nearby = @(Get-ChildItem -LiteralPath $dir -Filter "*.dds" -File -ErrorAction SilentlyContinue)
    $nearby += @(Get-ChildItem -LiteralPath $dir -Filter "*.dds" -File -Recurse -ErrorAction SilentlyContinue | Select-Object -First 100)
    foreach ($dds in $nearby) {
        $key = $dds.Name.ToLowerInvariant()
        if ($copied.ContainsKey($key)) { continue }
        Copy-Item -LiteralPath $dds.FullName -Destination (Join-Path $textureRoot $dds.Name) -Force
        $copied[$key] = $dds.FullName
    }
}
Write-Host "Yakindaki DDS kopyalandi: $($copied.Count)" -ForegroundColor Green
Write-Host "Texture klasoru: $textureRoot"
Write-Host "Bu adim Noesis calistirmaz; hizli bitmelidir." -ForegroundColor Yellow