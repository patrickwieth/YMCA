@echo off
setlocal EnableDelayedExpansion

call make.cmd all
if %ERRORLEVEL% NEQ 0 goto build_failed

FOR /F "tokens=1,2 delims==" %%A IN (mod.config) DO (set %%A=%%B)
if exist user.config (FOR /F "tokens=1,2 delims==" %%A IN (user.config) DO (set %%A=%%B))

if "!MOD_ID!"=="" goto badconfig
if "!ENGINE_VERSION!"=="" goto badconfig
if "!ENGINE_DIRECTORY!"=="" goto badconfig

set TEMPLATE_DIR=%CD%
set MAP_FILE=%TEMPLATE_DIR%\mods\ca\maps\behind-the-veil.oramap
set MAP_PACKAGE=behind-the-veil.oramap
set MOD_SEARCH_PATHS=%TEMPLATE_DIR%\mods,%~dp0mods,./mods

if not exist %ENGINE_DIRECTORY%\bin\OpenRA.exe goto noengine
>nul find %ENGINE_VERSION% %ENGINE_DIRECTORY%\VERSION || goto noengine
if not exist "%MAP_FILE%" goto nomap

cd %ENGINE_DIRECTORY%

bin\OpenRA.exe "Game.Mod=%MOD_ID%" "Launch.Map=%MAP_PACKAGE%" "Engine.EngineDir=.." "Engine.LaunchPath=%TEMPLATE_DIR%\tools\test-behind-the-veil.cmd" "Engine.ModSearchPaths=%MOD_SEARCH_PATHS%"
set ERROR=%ERRORLEVEL%
cd %TEMPLATE_DIR%

if %ERROR% NEQ 0 goto crashdialog
exit /b 0

:build_failed
echo Build failed.
exit /b %ERRORLEVEL%

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
