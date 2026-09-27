param(
    [string]$SourceRoot = "C:\Users\roxy\OneDrive\Masaüstü\Metin2BE-Client-master",
    [string]$OutputRoot = "",
    [switch]$PrepareBatch
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $SourceRoot)) {
    Write-Host "Kaynak klasör bulunamadı: $SourceRoot" -ForegroundColor Red
    exit 1
}

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $SourceRoot "_FBX_EXPORT"
}

New-Item -ItemType Directory -Force -Path $OutputRoot | Out-Null

$gr2Files = Get-ChildItem -LiteralPath $SourceRoot -Recurse -File -Filter "*.gr2" |
    Where-Object {
        $_.FullName -notmatch "\\.git(\\|$)" -and
        $_.FullName -notmatch "\\_FBX_EXPORT(\\|$)"
    }

$total = $gr2Files.Count
$commands = New-Object System.Collections.Generic.List[string]
$manifest = New-Object System.Collections.Generic.List[string]

Write-Host ""
Write-Host "METIN2 GR2 -> NOESIS BATCH HAZIRLAYICI" -ForegroundColor Cyan
Write-Host "Kaynak : $SourceRoot"
Write-Host "Cikis  : $OutputRoot"
Write-Host "GR2    : $total"
Write-Host ""

foreach ($file in $gr2Files) {
    $relative = $file.FullName.Substring($SourceRoot.Length).TrimStart('\')
    $relativeDir = Split-Path $relative -Parent
    $destDir = if ([string]::IsNullOrWhiteSpace($relativeDir)) {
        $OutputRoot
    } else {
        Join-Path $OutputRoot $relativeDir
    }

    New-Item -ItemType Directory -Force -Path $destDir | Out-Null

    $outFile = Join-Path $destDir ($file.BaseName + ".fbx")

    # Noesis Batch Process uses: source destination options
    $commands.Add(('"{0}" "{1}"' -f $file.FullName, $outFile))
    $manifest.Add(("{0}|{1}" -f $relative, $outFile))
}

$commandPath = Join-Path $OutputRoot "Noesis-Batch-Commands.txt"
$manifestPath = Join-Path $OutputRoot "Noesis-Batch-Manifest.txt"

$commands | Set-Content -Path $commandPath -Encoding UTF8
$manifest | Set-Content -Path $manifestPath -Encoding UTF8

Write-Host "Hazırlandı." -ForegroundColor Green
Write-Host "Komut listesi : $commandPath"
Write-Host "Manifest      : $manifestPath"
Write-Host ""
Write-Host "NOESIS'TE:"
Write-Host "1) Tools -> Batch Process"
Write-Host "2) Recursive kullanacaksan Folder batch ile de oluşturabilirsin."
Write-Host "3) Commands alanına Noesis-Batch-Commands.txt içeriğini yükle/yapıştır."
Write-Host "4) Destination yolları _FBX_EXPORT altında hazırlanmıştır."
Write-Host "5) Export'a bas."
Write-Host ""
Write-Host "Noesis GUI batch yöntemi kullanılacağı için ?cmode/CLI çağrısı yapılmıyor." -ForegroundColor Yellow
