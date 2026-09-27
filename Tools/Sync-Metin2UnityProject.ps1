$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$source = Join-Path $repoRoot "UnityClient\Assets\Metin2"
$project = Join-Path $repoRoot "Metin2Mobile"
$target = Join-Path $project "Assets\Metin2"

if (!(Test-Path $project)) { throw "Metin2Mobile klasoru bulunamadi: $project" }
if (!(Test-Path $source)) { throw "UnityClient\Assets\Metin2 klasoru bulunamadi." }

New-Item -ItemType Directory -Force -Path $target | Out-Null
Copy-Item -Path (Join-Path $source "*") -Destination $target -Recurse -Force

Write-Host ""
Write-Host "Metin2 Unity dosyalari senkronize edildi." -ForegroundColor Green
Write-Host "Unity projesi: $project"
Write-Host "Unity'yi kapatip yeniden acman yeterli."
