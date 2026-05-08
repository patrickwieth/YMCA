@echo off
setlocal EnableDelayedExpansion

set "BUILD_LOG=%TEMP%\ymca-make-output.log"
if exist "%BUILD_LOG%" del /f /q "%BUILD_LOG%" >nul 2>&1
if exist "%BUILD_LOG%" set "BUILD_LOG=%TEMP%\ymca-make-output-%RANDOM%.log"

pushd "%~dp0.."
call make.cmd all > "%BUILD_LOG%" 2>&1
set "BUILD_RC=%ERRORLEVEL%"
popd
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

FOR /F "usebackq tokens=1,2 delims==" %%A IN ("%~dp0..\mod.config") DO (set %%A=%%B)
if exist "%~dp0..\user.config" (FOR /F "usebackq tokens=1,2 delims==" %%A IN ("%~dp0..\user.config") DO (set %%A=%%B))

if "!MOD_ID!"=="" goto badconfig
if "!ENGINE_VERSION!"=="" goto badconfig
if "!ENGINE_DIRECTORY!"=="" goto badconfig

set TEMPLATE_DIR=%~dp0..
set MAP_PACKAGE=testmap1.oramap
set MOD_SEARCH_PATHS=%TEMPLATE_DIR%\mods,%~dp0mods,./mods

if not exist "%TEMPLATE_DIR%\mods\ca\maps\%MAP_PACKAGE%" goto nomap

if not exist "%~dp0..\engine\bin\OpenRA.exe" goto noengine
>nul findstr /C:"%ENGINE_VERSION%" "%~dp0..\engine\VERSION" || goto noengine

cd /d "%~dp0..\engine"

    bin\OpenRA.exe "Game.Mod=%MOD_ID%" "Engine.EngineDir=.." "Engine.LaunchPath=%TEMPLATE_DIR%\tools\test-game.cmd" "Engine.ModSearchPaths=%MOD_SEARCH_PATHS%" "PlayerFaction.Multi0=russia" "PlayerType.Multi0=Human"
set ERROR=%ERRORLEVEL%
cd /d "%TEMPLATE_DIR%"

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

:badconfig
echo Required mod.config variables are missing.
echo Ensure that MOD_ID ENGINE_VERSION and ENGINE_DIRECTORY are defined in your mod.config (or user.config) and try again.
pause
exit /b 1

::nosourcemap
echo Required map sources not found.
pause
exit /b 1

:mapcopyfailed
echo Failed to prepare the test map working copy.
pause
exit /b 1

:maprepackfailed
echo OpenRA.Utility --map repack failed.
pause
exit /b 1

echo Required map not found at %TEMPLATE_DIR%\mods\ca\maps\%MAP_PACKAGE%.
pause
exit /b 1

:crashdialog
echo ----------------------------------------
echo OpenRA exited with error level %ERROR%.
echo Check Documents\OpenRA\Logs for details.
echo ----------------------------------------
pause
exit /b %ERROR%
@endlocal
