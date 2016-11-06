@echo off
echo Automation monitor started
:BEGINNING
tasklist /nh /fi "imagename eq Automation.exe" | find /i "Automation.exe" > nul || goto AUTOMATE
ping 1.1.1.1 -n 1 -w 1000 > nul

GOTO BEGINNING

:AUTOMATE
echo ------------------------
echo Starting server
start "Automation server" "%~dp0Automation.appref-ms"
ping 1.1.1.1 -n 1 -w 10000 > nul
GOTO BEGINNING