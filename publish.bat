@echo off
cd /d "%~dp0"

echo === Building mod ===
dotnet build -c Release
if errorlevel 1 (
    echo.
    echo Build FAILED - nothing was uploaded.
    pause
    exit /b
)

echo.
echo === Uploading to Steam Workshop ===
"C:\Users\Yenleng\Desktop\steamcmd\steamcmd.exe" +login luviein +workshop_build_item "C:\Users\Yenleng\Desktop\steamcmd\more_customers_and_sellers.vdf" +quit

echo.
echo Done!
pause