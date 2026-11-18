@echo off
rem Compila XUMER TOOL con il compilatore C# incluso in Windows (.NET Framework 4)
cd /d "%~dp0"
if not exist bin mkdir bin
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set SRC=src\Lang.cs src\WinUsb.cs src\Xum1541.cs src\CbmDrive.cs src\FastCopy.cs src\Asm6502.cs src\Flasher.cs
set RES=/resource:lang\en.txt,lang.en.txt /resource:drivecode\s2.a65,s2.a65 /resource:drivecode\turboread1541.a65,turboread1541.a65 /resource:drivecode\turboread1571.a65,turboread1571.a65 /resource:drivecode\turbowrite1541.a65,turbowrite1541.a65 /resource:drivecode\turbowrite1571.a65,turbowrite1571.a65
set FONTS=/resource:fonts\PressStart2P-Regular.ttf,PressStart2P-Regular.ttf /resource:fonts\Sixtyfour.ttf,Sixtyfour.ttf /resource:fonts\VT323.ttf,VT323.ttf /resource:fonts\ShareTechMono.ttf,ShareTechMono.ttf /resource:fonts\JetBrainsMono.ttf,JetBrainsMono.ttf /resource:fonts\FiraCode.ttf,FiraCode.ttf /resource:fonts\IBMPlexMono.ttf,IBMPlexMono.ttf
set GUIRES=/win32icon:app.ico /resource:app.ico,app.ico /resource:app-icona.png,app-icona.png %FONTS%
set GUI=src\Tools.cs src\Widgets.cs src\Vice.cs src\About.cs src\Preferences.cs src\Fun.cs src\MainForm.cs
"%CSC%" /nologo /utf8output /target:winexe /optimize+ /out:bin\XumerTool.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll %RES% %GUIRES% %SRC% %GUI%
if errorlevel 1 (echo Compilazione FALLITA & pause & exit /b 1)
"%CSC%" /nologo /utf8output /target:exe /out:bin\xumtest.exe %RES% %SRC% test\TestMain.cs
echo Fatto: bin\XumerTool.exe
