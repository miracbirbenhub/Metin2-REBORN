$ErrorActionPreference = "Stop"
$desktop = [Environment]::GetFolderPath("Desktop")
$noesis = Join-Path $desktop "shared_3d_exporting\noesis\noesis\Noesis.exe"
$sourceRoot = Join-Path $desktop "Metin2BE-Client-master\objects\ymir work\terrainmaps\b"
$outputRoot = Join-Path (Join-Path $desktop "Metin2BE-Client-master") "_BLUE1_PNG"
$files = @(
"field\field 01.dds","field\field 02.dds","field\field 03.dds","field\field 04.dds",
"grass\grass 01.dds","grass\grass 02.dds","grass\grass 03.dds",
"stone\stone01.dds","stone\stone02.dds","stone\stone03.dds","stone\stone04.dds",
"tile\tile01.dds","tile\tile02.dds","tile\tile03.dds",
"beach\beach sand 01.dds","beach\beach sand 02.dds","beach\beach sand 03.dds")
if (!(Test-Path $noesis)) { throw "Noesis bulunamadi: $noesis" }
New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null
foreach ($rel in $files) {
  $input = Join-Path $sourceRoot $rel
  $name = [IO.Path]::GetFileNameWithoutExtension($rel) + ".png"
  $output = Join-Path $outputRoot $name
  if (!(Test-Path $input)) { throw "DDS bulunamadi: $input" }
  & $noesis "?cmode" $input $output
  if ($LASTEXITCODE -ne 0 -or !(Test-Path $output)) { throw "Noesis PNG donusumu basarisiz: $input" }
  Write-Host "OK $name"
}
Write-Host "BLUE 1 PNG donusumu tamamlandi: $outputRoot" -ForegroundColor Green
