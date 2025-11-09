@echo off
setlocal EnableDelayedExpansion

set "BUILD_LOG=%TEMP%\ymca-make-output.log"
if exist "%BUILD_LOG%" del /f /q "%BUILD_LOG%"

call make.cmd all > "%BUILD_LOG%" 2>&1
set "BUILD_RC=%ERRORLEVEL%"
type "%BUILD_LOG%"
echo.

if not "%BUILD_RC%"=="0" goto build_failed

findstr /C:"Build FAILED." "%BUILD_LOG%" >nul 2>&1
if %ERRORLEVEL% EQU 0 goto build_failed

findstr /C:": error" "%BUILD_LOG%" >nul 2>&1
if %ERRORLEVEL% EQU 0 goto build_failed

del /f /q "%BUILD_LOG%" >nul 2>&1

call launch-game.cmd
exit /b 0

:build_failed
echo Build failed. Skipping game launch.
pause
exit /b %BUILD_RC%
