@echo off
rem Crea l'installer bin\XUMER-TOOL-Setup.exe (prima compila l'applicazione con build.bat)
cd /d "%~dp0"
call "%~dp0build.bat"
if errorlevel 1 exit /b 1
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set PAY=/resource:lang\en.txt,lang.en.txt /resource:bin\XumerTool.exe,payload/XumerTool.exe /resource:installer\LEGGIMI.txt,payload/LEGGIMI.txt /resource:installer\README.txt,payload/README.txt /resource:app-icona.png,logo.png /resource:LICENSE,payload/LICENSE.txt /resource:licenses\BSD-OpenCBM-d64copy.txt,payload/Licenze/BSD-OpenCBM-d64copy.txt
for %%f in (fonts\OFL-*.txt) do call set PAY=%%PAY%% /resource:%%f,payload/Licenze/%%~nxf
"%CSC%" /nologo /utf8output /target:winexe /optimize+ /win32icon:app.ico /out:bin\XUMER-TOOL-Setup.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll %PAY% installer\Setup.cs src\Lang.cs
if errorlevel 1 (echo Creazione installer FALLITA & pause & exit /b 1)
echo Fatto: bin\XUMER-TOOL-Setup.exe
rem (installer messo da parte per ora: la versione pubblicata e' quella portatile, build-portable.bat)
