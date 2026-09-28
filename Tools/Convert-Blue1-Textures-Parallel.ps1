param([string]$TextureRoot="")
$ErrorActionPreference="Stop"
$desktop=[Environment]::GetFolderPath("Desktop")
if([string]::IsNullOrWhiteSpace($TextureRoot)){$TextureRoot=Join-Path $desktop "Metin2-REBORN-Git\Metin2Mobile\Assets\Metin2\Imported\Blue1Required\Textures"}
$noesis=Join-Path $desktop "shared_3d_exporting\noesis\noesis\Noesis.exe"
if(!(Test-Path $TextureRoot)){throw "TextureRoot bulunamadi"}
if(!(Test-Path $noesis)){throw "Noesis bulunamadi"}
$dds=@(Get-ChildItem -LiteralPath $TextureRoot -Filter "*.dds" -File)
$temp="C:\M2Blue1_Noesis\TexturesFast"
$in=Join-Path $temp "DDS"; $out=Join-Path $temp "PNG"
Remove-Item $temp -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $in,$out|Out-Null
Write-Host "BLUE 1 TEXTURE CONVERTER - PARALLEL"
Write-Host "DDS: $($dds.Count)"
$batch=8;$done=0;$jobs=@()
foreach($file in $dds){
  $name=[IO.Path]::GetFileNameWithoutExtension($file.Name)
  $inFile=Join-Path $in ($name+".dds");$outFile=Join-Path $out ($name+".png")
  Copy-Item $file.FullName $inFile -Force
  $args='?cmode "'+$inFile+'" "'+$outFile+'"'
  $p=Start-Process -FilePath $noesis -ArgumentList $args -WindowStyle Hidden -PassThru
  $jobs+=[pscustomobject]@{P=$p;O=$outFile}
  if($jobs.Count -ge $batch){
    foreach($j in $jobs){$j.P.WaitForExit();if(Test-Path $j.O){$done++}}
    $jobs=@();Write-Host "Donusturuldu: $done / $($dds.Count)"
  }
}
foreach($j in $jobs){$j.P.WaitForExit();if(Test-Path $j.O){$done++}}
foreach($p in Get-ChildItem $out -Filter "*.png" -File){Copy-Item $p.FullName (Join-Path $TextureRoot $p.Name) -Force}
Write-Host "PNG basarili: $done / $($dds.Count)" -ForegroundColor Green
Write-Host "TAMAMLANDI" -ForegroundColor Green
