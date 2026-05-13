@echo off
set "BASE_PATH=%~dp0"
set "SERVICE_EXE=TeamSpot.Service.exe"

sc.exe create "TeamSpot.Service" binpath="%BASE_PATH%%SERVICE_EXE%"
sc.exe description "TeamSpot.Service" "Physical interface to MS Teams"

:: TODO: configure service: https://learn.microsoft.com/en-us/dotnet/core/extensions/windows-service#configure-the-windows-service

pause