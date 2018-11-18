@echo off
cls
:start
echo Starting server...

Human.exe -batchmode -nographics -dedicated -logfile output_log.txt -servername "My uMod Server" -maxplayers 10
@rem Human.exe -logfile output_log.txt -servername "My uMod Server" -maxplayers 10

echo.
echo Restarting server...
timeout /t 10
echo.
goto start
