param(
    [string]$SourceRoot = "C:\Users\roxy\OneDrive\Masaüstü\Metin2BE-Client-master",
    [string]$NoesisExe = "C:\Users\roxy\OneDrive\Masaüstü\shared_3d_exporting\noesis\noesis\Noesis.exe",
    [string]$OutputRoot = ""
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $NoesisExe)) {
    Write-Host "Noesis bulunamadı:" -ForegroundColor Red
    Write-Host $NoesisExe
    Write-Host ""
    Write-Host "Noesis.exe'nin gerçek yolunu -NoesisExe ile ver."
    exit 1
}

if (-not (Test-Path -LiteralPath $SourceRoot)) {
    Write-Host "Kaynak klasör bulunamadı: $SourceRoot" -ForegroundColor Red
    exit 1
}

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $SourceRoot "_FBX_EXPORT"
}

New-Item -ItemType Directory -Force -Path $OutputRoot | Out-Null

$gr2Files = Get-ChildItem -LiteralPath $SourceRoot -Recurse -File -Filter "*.gr2" |
    Where-Object { $_.FullName -notmatch "\\\.git(\\|$)" -and $_.FullName -notmatch "\\_FBX_EXPORT(\\|$)" }

$total = $gr2Files.Count
$ok = 0
$failed = 0
$index = 0
$log = New-Object System.Collections.Generic.List[string]

Write-Host ""
Write-Host "METIN2 GR2 -> FBX TOPLU DONUSTURUCU" -ForegroundColor Cyan
Write-Host "Kaynak : $SourceRoot"
Write-Host "Cikis  : $OutputRoot"
Write-Host "Noesis : $NoesisExe"
Write-Host "GR2    : $total"
Write-Host ""

foreach ($file in $gr2Files) {
    $index++

    $relative = $file.FullName.Substring($SourceRoot.Length).TrimStart('\')
    $relativeDir = Split-Path $relative -Parent
    $destDir = if ([string]::IsNullOrWhiteSpace($relativeDir)) { $OutputRoot } else { Join-Path $OutputRoot $relativeDir }

    New-Item -ItemType Directory -Force -Path $destDir | Out-Null

    $outFile = Join-Path $destDir ($file.BaseName + ".fbx")

    Write-Progress -Activity "GR2 -> FBX" -Status "$index / $total : $relative" -PercentComplete (($index / [math]::Max($total,1)) * 100)

    if (Test-Path -LiteralPath $outFile) {
        $ok++
        $log.Add("SKIP|$relative|already exists")
        continue
    }

    & $NoesisExe "?cmode" $file.FullName $outFile "-fbxnewexport" 2>&1 | Out-Null
    $exitCode = $LASTEXITCODE

    if ($exitCode -eq 0 -and (Test-Path -LiteralPath $outFile)) {
        $ok++
        $log.Add("OK|$relative|$outFile")
    } else {
        $failed++
        $log.Add("FAIL|$relative|exit=$exitCode")
    }
}

$logPath = Join-Path $OutputRoot "conversion-log.txt"
$log | Set-Content -Path $logPath -Encoding UTF8

Write-Progress -Activity "GR2 -> FBX" -Completed
Write-Host ""
Write-Host "BITTI." -ForegroundColor Green
Write-Host "Toplam : $total"
Write-Host "Basari : $ok"
Write-Host "Hata  : $failed"
Write-Host "Log   : $logPath"
