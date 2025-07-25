@echo off
echo ============================================
echo  ������ Telegram Chat Archiver Web App
echo ============================================
cd /d "Telegram.HostedApp"
echo ������� ����������: %CD%
echo.

echo ��������� ������� ����������� ������...
if exist "wwwroot\index.html" (
    echo ? index.html ������
) else (
    echo ? index.html �� ������ � wwwroot
)

if exist "appsettings.json" (
    echo ? appsettings.json ������
) else (
    echo ? appsettings.json �� ������
)

echo.
echo ��������� ��� �������...
findstr "Microsoft.NET.Sdk.Web" Telegram.HostedApp.csproj >nul
if %errorlevel%==0 (
    echo ? ������ �������� ��� ���-����������
) else (
    echo ? ������ �� �������� ��� ���-����������
)

echo.
echo ============================================
echo  ������ ���-�������
echo ============================================
echo ����� ������� �������� � ��������:
echo.
echo   http://localhost:5000
echo.
echo ��� ��������� ������� Ctrl+C
echo.
pause
echo.

dotnet run --verbosity normal

echo.
echo ============================================
echo ������� ����� ������� ��� ������...
pause