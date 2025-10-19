@echo off
setlocal EnableDelayedExpansion

set "BUILD_LOG=%TEMP%\ymca-make-output.log"
if exist "%BUILD_LOG%" del /f /q "%BUILD_LOG%"

call make.cmd all > "%BUILD_LOG%" 2>&1
set "BUILD_RC=%ERRORLEVEL%"
type "%BUILD_LOG%"
echo.
if %BUILD_RC% NEQ 0 (
	set "BUILD_FAILED_RC=%BUILD_RC%"
	goto build_failed
)

findstr /C:"Build FAILED." "%BUILD_LOG%" >nul 2>&1
if %ERRORLEVEL% EQU 0 (
	set "BUILD_FAILED_RC=1"
	goto build_failed
)

findstr /C:": error" "%BUILD_LOG%" >nul 2>&1
if %ERRORLEVEL% EQU 0 (
	set "BUILD_FAILED_RC=1"
	goto build_failed
)

del /f /q "%BUILD_LOG%" >nul 2>&1

FOR /F "tokens=1,2 delims==" %%A IN (mod.config) DO (set %%A=%%B)
if exist user.config (FOR /F "tokens=1,2 delims==" %%A IN (user.config) DO (set %%A=%%B))

if "!MOD_ID!"=="" goto badconfig
if "!ENGINE_VERSION!"=="" goto badconfig
if "!ENGINE_DIRECTORY!"=="" goto badconfig

set TEMPLATE_DIR=%CD%
set MAP_FILE=C:\Users\lordesfairgenug\AppData\Roaming\OpenRA\maps\ca\61290389ccbcc5070d8029110887b62bdb1edb39\testmap.oramap
set MAP_PACKAGE=testmap.oramap
set MOD_SEARCH_PATHS=%TEMPLATE_DIR%\mods,%~dp0mods,./mods

if not exist %ENGINE_DIRECTORY%\bin\OpenRA.exe goto noengine
>nul find %ENGINE_VERSION% %ENGINE_DIRECTORY%\VERSION || goto noengine
if not exist "%MAP_FILE%" goto nomap

cd %ENGINE_DIRECTORY%

	bin\OpenRA.exe "Game.Mod=%MOD_ID%" "Launch.Map=%MAP_PACKAGE%" "Engine.EngineDir=.." "Engine.LaunchPath=%TEMPLATE_DIR%\tools\test-game.cmd" "Engine.ModSearchPaths=%MOD_SEARCH_PATHS%"
set ERROR=%ERRORLEVEL%
cd %TEMPLATE_DIR%

if %ERROR% NEQ 0 goto crashdialog
exit /b 0

:build_failed
echo Build failed.
if defined BUILD_FAILED_RC (
	exit /b %BUILD_FAILED_RC%
) else (
	exit /b %ERRORLEVEL%
)

:noengine
echo Required engine files not found.
echo Run make all in the mod directory to fetch and build the required files, then try again.
pause
exit /b 1

:nomap
echo Required map not found at %MAP_FILE%.
pause
exit /b 1

:badconfig
echo Required mod.config variables are missing.
echo Ensure that MOD_ID ENGINE_VERSION and ENGINE_DIRECTORY are defined in your mod.config (or user.config) and try again.
pause
exit /b 1

:crashdialog
echo ----------------------------------------
echo OpenRA exited with error level %ERROR%.
echo Check Documents\OpenRA\Logs for details.
echo ----------------------------------------
pause
exit /b %ERROR%
