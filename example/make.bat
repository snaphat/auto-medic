@echo off
del *.exe *.bak *.config
REM setup powershell to print colors
set echo_color=powershell -NoProfile write-host -fore Yellow
REM Look for de4dotp.exe in the path.
for %%i in (de4dotp.exe) do set de4dotp=%%~$PATH:i
REM Compile sample and auto-medic

%echo_color% ------------------------------------------
echo Compiling things:
echo.
csc.exe /out:a.exe sample.cs /optimize+ /nologo
csc.exe /out:Auto-Medic.exe /r:"%de4dotp%" "..\automedic.cs" auto-medic.cs /optimize+ /nologo
ILRepack.exe Auto-Medic.exe "%de4dotp%" /out:Auto-Medic.exe
echo.

%echo_color% ------------------------------------------
echo Executing Program Before Modification:
echo.
a.exe

%echo_color% ------------------------------------------
echo Running auto-medic:
echo.
echo "" | Auto-Medic.exe
echo.

%echo_color% ------------------------------------------
echo Executing Program AFTER Modification:
echo.
a.exe

%echo_color% ------------------------------------------
