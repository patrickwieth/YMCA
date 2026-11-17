@echo off
setlocal
title YMCA - Building Test (isometric / modular)

rem Run only this worktree's launcher, independent of the current directory.
if not exist "%~dp0tools\test-game.cmd" (
    echo Missing launcher: %~dp0tools\test-game.cmd
    pause
    exit /b 1
)

echo Starting Building Grid Test (All Factions)...
echo The current game is built first. No lobby or map selection is needed.
echo.
call "%~dp0tools\test-game.cmd" "%~1"
set "RESULT=%ERRORLEVEL%"
if not "%RESULT%"=="0" (
    echo.
    echo Building test failed. See the error output above.
    pause
)
endlocal & exit /b %RESULT%
