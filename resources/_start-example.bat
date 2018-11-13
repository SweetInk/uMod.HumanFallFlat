@echo off
cls
:start
echo Starting server...

Human.exe -batchmode -nographics -bind 0.0.0.0 -port 27015 -maxplayers 10

echo.
echo Restarting server...
timeout /t 10
echo.
goto start
