@echo off
echo ============================================
echo  Запуск Telegram Chat Archiver Web App
echo ============================================
cd /d "Telegram.HostedApp"
echo Текущая директория: %CD%
echo.

echo Проверяем наличие необходимых файлов...
if exist "wwwroot\index.html" (
    echo ? index.html найден
) else (
    echo ? index.html НЕ найден в wwwroot
)

if exist "appsettings.json" (
    echo ? appsettings.json найден
) else (
    echo ? appsettings.json НЕ найден
)

echo.
echo Проверяем тип проекта...
findstr "Microsoft.NET.Sdk.Web" Telegram.HostedApp.csproj >nul
if %errorlevel%==0 (
    echo ? Проект настроен как веб-приложение
) else (
    echo ? Проект НЕ настроен как веб-приложение
)

echo.
echo ============================================
echo  ЗАПУСК ВЕБ-СЕРВЕРА
echo ============================================
echo После запуска откройте в браузере:
echo.
echo   http://localhost:5000
echo.
echo Для остановки нажмите Ctrl+C
echo.
pause
echo.

dotnet run --verbosity normal

echo.
echo ============================================
echo Нажмите любую клавишу для выхода...
pause