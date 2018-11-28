@echo off
cls
:start
echo Starting server...

@rem Human.exe -batchmode -nographics -dedicated -logfile output_log.txt -servername "My uMod Server" -maxplayers 10
Human.exe -logfile output_log.txt -servername "My uMod Server" -maxplayers 10

echo.
echo Restarting server...
timeout /t 10
echo.
goto start
