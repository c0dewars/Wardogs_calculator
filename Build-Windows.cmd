@echo off
setlocal
cd /d "%~dp0"
dotnet --list-sdks 2>nul | findstr /r "^[89]\. ^[1-9][0-9]\." >nul
if errorlevel 1 (
  echo Install the .NET 8 SDK or newer SDK, then run this file again.
  echo The .NET Runtime alone cannot build source code.
  pause
  exit /b 1
)
echo Building a standalone Windows x64 executable...
dotnet publish WardogsCalculator.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false -o publish
if errorlevel 1 goto failed
echo Checking coordinate parsing and calculations...
start "" /wait "publish\WardogsCalculator.exe" --self-test
if errorlevel 1 goto failed
echo Ready: publish\WardogsCalculator.exe
echo You can copy this executable to another Windows x64 PC. No installer is needed.
explorer "publish"
pause
exit /b 0
:failed
echo Build or self-test failed. See the error above.
pause
exit /b 1
