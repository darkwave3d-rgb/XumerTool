@echo off
rem Crea la versione da pubblicare: bin\XumerTool-<versione>-portable.zip (prima compila con build.bat)
cd /d "%~dp0"
call "%~dp0build.bat"
if errorlevel 1 exit /b 1
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0make-portable.ps1"
if errorlevel 1 (echo Creazione zip portatile FALLITA & pause & exit /b 1)
