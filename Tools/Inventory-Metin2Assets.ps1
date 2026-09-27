$ErrorActionPreference = "Stop"

param(
    [string]$SourceRoot = ""
)

if ([string]::IsNullOrWhiteSpace($SourceRoot)) {
    $SourceRoot = Join-Path $PSScriptRoot ".."
}

$SourceRoot = (Resolve-Path $SourceRoot).Path
$out = Join-Path $SourceRoot "Docs\METIN2_ASSET_INVENTORY.txt"

$files = Get-ChildItem $SourceRoot -Recurse -File -ErrorAction SilentlyContinue
$groups = $files | Group-Object Extension | Sort-Object Count -Descending

$lines = @()
$lines += "METIN2-REBORN ASSET INVENTORY"
$lines += "Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
$lines += "Root: $SourceRoot"
$lines += ""
$lines += "FILE COUNTS BY EXTENSION"
$lines += "========================="
foreach ($g in $groups) {
    $ext = if ([string]::IsNullOrWhiteSpace($g.Name)) { "[no extension]" } else { $g.Name }
    $lines += ("{0,8}  {1}" -f $g.Count, $ext)
}
$lines += ""
$lines += "IMPORTANT ASSET FILES"
$lines += "====================="
$interesting = @(".gr2",".fbx",".obj",".dae",".gltf",".glb",".dds",".png",".tga",".jpg",".jpeg",".txt",".xml",".json",".msm",".mde",".msa",".mse",".atr",".prb")
foreach ($ext in $interesting) {
    $matches = $files | Where-Object { $_.Extension -ieq $ext }
    if ($matches.Count -gt 0) {
        $lines += ""
        $lines += "$ext ($($matches.Count))"
        foreach ($f in $matches | Select-Object -First 200) {
            $lines += $f.FullName
        }
        if ($matches.Count -gt 200) { $lines += "... truncated at 200 files ..." }
    }
}

$lines += ""
$lines += "TOP DIRECTORIES"
$lines += "==============="
$dirs = Get-ChildItem $SourceRoot -Directory -Recurse -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch "\\.git(\\|$)" -and $_.FullName -notmatch "\\Library(\\|$)" }
foreach ($d in $dirs | Select-Object -First 500) {
    $lines += $d.FullName
}

$lines | Set-Content -Path $out -Encoding UTF8
Write-Host "Inventory created: $out"
Write-Host ""
$groups | Format-Table Count, Name -AutoSize
