$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$source = Join-Path $repoRoot "UnityClient\Assets\Metin2"
$project = Join-Path $repoRoot "Metin2Mobile"
$target = Join-Path $project "Assets\Metin2"

# Local Noesis output. This is intentionally outside the Git repository.
$externalExport = "C:\Users\roxy\OneDrive\Masaüstü\Metin2BE-Client-master\_FBX_EXPORT"
$importedTarget = Join-Path $target "Imported"

if (!(Test-Path $project)) { throw "Metin2Mobile klasoru bulunamadi: $project" }
if (!(Test-Path $source)) { throw "UnityClient\Assets\Metin2 klasoru bulunamadi." }

New-Item -ItemType Directory -Force -Path $target | Out-Null
Copy-Item -Path (Join-Path $source "*") -Destination $target -Recurse -Force

$copiedFbx = 0
if (Test-Path $externalExport) {
    New-Item -ItemType Directory -Force -Path $importedTarget | Out-Null

    Get-ChildItem -LiteralPath $externalExport -Filter "*.fbx" -Recurse -File |
        ForEach-Object {
            $relative = $_.FullName.Substring($externalExport.Length).TrimStart('\')
            $destination = Join-Path $importedTarget $relative
            $destinationDir = Split-Path $destination -Parent

            New-Item -ItemType Directory -Force -Path $destinationDir | Out-Null
            Copy-Item -LiteralPath $_.FullName -Destination $destination -Force
            $copiedFbx++
        }

    Write-Host "Noesis FBX aktarildi: $copiedFbx" -ForegroundColor Cyan
} else {
    Write-Host "Noesis _FBX_EXPORT bulunamadi; sadece Git dosyalari senkronize edildi." -ForegroundColor Yellow
    Write-Host "Beklenen: $externalExport"
}

Write-Host ""
Write-Host "Metin2 Unity dosyalari senkronize edildi." -ForegroundColor Green
Write-Host "Unity projesi: $project"
Write-Host "Imported FBX hedefi: $importedTarget"
Write-Host "Unity'yi kapatip yeniden acman yeterli."
