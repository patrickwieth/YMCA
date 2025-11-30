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
set MAP_NAME=testmap
set MAP_HASH=61290389ccbcc5070d8029110887b62bdb1edb39
set MAP_ROOT=C:\Users\Patrick\OneDrive\Dokumente\OpenRA\maps\ca\%MAP_HASH%
set MAP_SOURCE_DIR=%MAP_ROOT%\%MAP_NAME%
set MAP_WORK_DIR=%TEMPLATE_DIR%\tools\%MAP_NAME%_pack
set MAP_WORK_FILE=%MAP_WORK_DIR%.oramap
set MAP_OUTPUT=%TEMPLATE_DIR%\tools\%MAP_NAME%.oramap
set MOD_SEARCH_PATHS=%TEMPLATE_DIR%\mods,%~dp0mods,./mods
for %%I in ("%ENGINE_DIRECTORY%") do set "ENGINE_DIR=%%~fI"

if not exist "%MAP_SOURCE_DIR%" goto nosourcemap

if exist "%MAP_WORK_DIR%" rmdir /s /q "%MAP_WORK_DIR%"
robocopy "%MAP_SOURCE_DIR%" "%MAP_WORK_DIR%" /MIR >nul
if %ERRORLEVEL% GEQ 8 goto mapcopyfailed

if exist "%MAP_WORK_FILE%" del /f /q "%MAP_WORK_FILE%" >nul 2>&1
set "MAP_TEMP_ZIP=%MAP_WORK_FILE%.zip"
if exist "%MAP_TEMP_ZIP%" del /f /q "%MAP_TEMP_ZIP%" >nul 2>&1
for %%I in ("%MAP_WORK_DIR%") do set "_PS_MAP_DIR=%%~fI"
for %%I in ("%MAP_TEMP_ZIP%") do set "_PS_MAP_FILE=%%~fI"
powershell -NoProfile -Command "Set-StrictMode -Version Latest; Set-Location -LiteralPath \"%_PS_MAP_DIR%\"; Compress-Archive -Path * -DestinationPath \"%_PS_MAP_FILE%\" -Force"
set MAP_PACK_RC=%ERRORLEVEL%
set "_PS_MAP_DIR="
set "_PS_MAP_FILE="
if %MAP_PACK_RC% EQU 0 if exist "%MAP_TEMP_ZIP%" move /Y "%MAP_TEMP_ZIP%" "%MAP_WORK_FILE%" >nul
set "MAP_TEMP_ZIP="
if %MAP_PACK_RC% NEQ 0 goto maprepackfailed

if not exist "%MAP_WORK_FILE%" goto nomap

copy /Y "%MAP_WORK_FILE%" "%MAP_OUTPUT%" >nul
if %ERRORLEVEL% NEQ 0 goto mapcopyfailed

del /f /q "%MAP_WORK_FILE%" >nul 2>&1

set MAP_PACKAGE=%MAP_NAME%.oramap

if not exist %ENGINE_DIRECTORY%\bin\OpenRA.exe goto noengine
>nul find %ENGINE_VERSION% %ENGINE_DIRECTORY%\VERSION || goto noengine

cd %ENGINE_DIRECTORY%

    bin\OpenRA.exe "Game.Mod=%MOD_ID%" "Launch.Map=%MAP_PACKAGE%" "Engine.EngineDir=.." "Engine.LaunchPath=%TEMPLATE_DIR%\tools\test-game.cmd" "Engine.ModSearchPaths=%MOD_SEARCH_PATHS%" "PlayerFaction.Multi0=russia" "PlayerType.Multi0=Human"
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

:badconfig
echo Required mod.config variables are missing.
echo Ensure that MOD_ID ENGINE_VERSION and ENGINE_DIRECTORY are defined in your mod.config (or user.config) and try again.
pause
exit /b 1

:nosourcemap
echo Required map sources not found at %MAP_SOURCE_DIR%.
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

:nomap
echo Required map not found at %MAP_OUTPUT%.
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
