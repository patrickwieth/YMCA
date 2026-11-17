@echo off
setlocal
pushd "%~dp0"
call make.cmd all
if errorlevel 1 goto failed
python tools\activate_liberty_shellmap.py
if errorlevel 1 goto failed
call launch-game.cmd
set "RESULT=%ERRORLEVEL%"
popd
exit /b %RESULT%
:failed
echo Liberty preparation failed. Close running YMCA instances normally and retry.
popd
pause
exit /b 1
