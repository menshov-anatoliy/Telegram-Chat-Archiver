@echo off
echo Check running processes Telegram.HostedApp...

for /f "tokens=2 delims=," %%i in ('tasklist /fo csv /fi "imagename eq Telegram.HostedApp.exe" 2^>nul ^| find "Telegram.HostedApp.exe"') do (
    echo Найден процесс с PID: %%i
    set /p choice="Kill proccess %%i? (y/n): "
    if /i "!choice!"=="y" (
        taskkill /pid %%i /f
        echo Proccess %%i killed.
    )
)

echo.
echo Check processes .NET Host...
for /f "tokens=2 delims=," %%i in ('tasklist /fo csv /fi "imagename eq dotnet.exe" 2^>nul ^| find "dotnet.exe"') do (
    echo Finded .NET proccess PID: %%i
)

echo.
echo Finish.
pause