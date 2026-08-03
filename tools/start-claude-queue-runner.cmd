@echo off
setlocal

set "SCRIPT_DIR=%~dp0"

where pwsh.exe >nul 2>nul
if not errorlevel 1 (
    pwsh.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%claude-queue-runner.ps1" %*
    exit /b %ERRORLEVEL%
)

powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%claude-queue-runner.ps1" %*
exit /b %ERRORLEVEL%
