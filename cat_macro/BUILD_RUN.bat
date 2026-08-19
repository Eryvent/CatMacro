@echo off
setlocal enabledelayedexpansion
echo.
echo =====================================================================
echo           MACRO RECORDER - Build and Deploy
echo =====================================================================
echo.

dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET SDK not installed!
    echo Install from: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo Restoring packages...
dotnet restore
if errorlevel 1 (
    echo Build failed!
    pause
    exit /b 1
)

echo Building project...
dotnet build -c Release --no-restore
if errorlevel 1 (
    echo Build failed!
    pause
    exit /b 1
)

echo Publishing application...
dotnet publish -c Release -o ./publish --no-build
if errorlevel 1 (
    echo Publish failed!
    pause
    exit /b 1
)

echo.
echo =====================================================================
echo BUILD SUCCESSFUL!
echo =====================================================================
echo.
echo Application location: publish\CatMacro.exe
echo.
echo Launching application...
echo.

start "" "publish\CatMacro.exe"
pause
