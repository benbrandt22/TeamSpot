@echo off
set "BASE_PATH=%~dp0"
set "SERVICE_EXE=TeamSpot.Service.exe"

sc.exe create "TeamSpot.Service" binpath="%BASE_PATH%%SERVICE_EXE%" start=auto
sc.exe description "TeamSpot.Service" "Physical interface to MS Teams"

:: Service configuration: https://learn.microsoft.com/en-us/dotnet/core/extensions/windows-service#configure-the-windows-service
sc.exe failure "TeamSpot.Service" reset= 0 actions= restart/60000/restart/60000/run/1000

sc.exe start "TeamSpot.Service"

pause