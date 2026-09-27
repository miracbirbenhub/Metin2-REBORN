@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Tools\Sync-Metin2UnityProject.ps1"
if errorlevel 1 (
  echo.
  echo SENKRONIZASYON BASARISIZ.
  pause
  exit /b 1
)
echo.
echo Unity projesi hazir. Unity Hub'dan Metin2Mobile klasorunu ac.
pause
