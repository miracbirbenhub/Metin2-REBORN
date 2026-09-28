param(
    [string]$TextureRoot = ""
)

$ErrorActionPreference = "Stop"
$desktop = [Environment]::GetFolderPath("Desktop")
if ([string]::IsNullOrWhiteSpace($TextureRoot)) {
    $TextureRoot = Join-Path $desktop "Metin2-REBORN-Git\Metin2Mobile\Assets\Metin2\Imported\Blue1Required\Textures"
}

if (-not (Test-Path -LiteralPath $TextureRoot)) {
    throw "TextureRoot bulunamadi: $TextureRoot"
}

$noesis = Join-Path $desktop "shared_3d_exporting\noesis\noesis\Noesis.exe"
if (-not (Test-Path -LiteralPath $noesis)) {
    throw "Noesis.exe bulunamadi: $noesis"
}

$dds = @(Get-ChildItem -LiteralPath $TextureRoot -Filter "*.dds" -File)
if ($dds.Count -eq 0) {
    Write-Host "DDS bulunamadi. Texture hazirlama adimi zaten PNG uretmis olabilir." -ForegroundColor Yellow
    exit 0
}

$tempRoot = "C:\M2Blue1_Noesis\TexturesFast"
$tempIn = Join-Path $tempRoot "DDS"
$tempOut = Join-Path $tempRoot "PNG"
Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $tempIn,$tempOut | Out-Null

Write-Host "BLUE 1 TEXTURE CONVERTER" -ForegroundColor Cyan
Write-Host "DDS: $($dds.Count)"
Write-Host ""

$done = 0
foreach ($file in $dds) {
    $name = [IO.Path]::GetFileNameWithoutExtension($file.Name)
    $inFile = Join-Path $tempIn ($name + ".dds")
    $outFile = Join-Path $tempOut ($name + ".png")
    Copy-Item -LiteralPath $file.FullName -Destination $inFile -Force

    cmd /c ""$noesis" "?cmode" "$inFile" "$outFile" 2>nul" | Out-Null

    if (Test-Path -LiteralPath $outFile) {
        Copy-Item -LiteralPath $outFile -Destination (Join-Path $TextureRoot ($name + ".png")) -Force
        $done++
    }
}

Write-Host ""
Write-Host "PNG basarili: $done / $($dds.Count)" -ForegroundColor Green
Write-Host "Unity texture klasoru: $TextureRoot"
Write-Host "TAMAMLANDI" -ForegroundColor Green
