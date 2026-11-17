@echo off
setlocal
pushd "%~dp0"
rem Build before selecting the map: an old CA DLL must never load the new driver.
call make.cmd all
if errorlevel 1 goto failed
python tools\activate_two_sides_shellmap.py
if errorlevel 1 goto failed
call launch-game.cmd
set "RESULT=%ERRORLEVEL%"
popd
exit /b %RESULT%
:failed
echo.
echo Shellmap preparation failed. Close any running YMCA instances normally and retry.
echo The previous shellmap selection was not intentionally replaced before validation.
popd
pause
exit /b 1
