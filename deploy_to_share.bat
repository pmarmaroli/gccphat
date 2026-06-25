@echo off
setlocal enabledelayedexpansion

REM ============================================================================
REM  deploy_to_share.bat
REM  Publishes a self-contained, single-file gccphat.exe and copies ONLY what
REM  the team needs to run the tool into the shared folder.
REM
REM  The published gccphat.exe is fully standalone: it embeds the .NET runtime,
REM  so machines that run it need NOTHING installed (no .NET, no DLLs).
REM ============================================================================

REM --- Configuration ----------------------------------------------------------
set "REPO=%~dp0"
set "PROJECT=%REPO%gccphat.csproj"
set "PUBLISH_DIR=%REPO%_publish_share"
set "DEST=\\ic3-bm-2\BM-Tools\gccphat"
set "SAMPLE=%REPO%stereo_noise.wav"

echo ============================================================
echo  GCC-PHAT - Deploiement vers le partage equipe
echo  Destination : %DEST%
echo ============================================================
echo.

REM --- Step 1: publish self-contained single-file exe -------------------------
echo [1/4] Publication de gccphat.exe (self-contained, single-file)...
dotnet publish "%PROJECT%" -c Release -r win-x64 --self-contained true ^
    -p:PublishSingleFile=true -o "%PUBLISH_DIR%"
if errorlevel 1 (
    echo.
    echo [ERREUR] La publication a echoue. Verifiez que le SDK .NET est installe.
    goto :fail
)
if not exist "%PUBLISH_DIR%\gccphat.exe" (
    echo [ERREUR] gccphat.exe introuvable apres publication.
    goto :fail
)

REM --- Step 2: ensure destination folder --------------------------------------
echo.
echo [2/4] Preparation du dossier de destination...
if not exist "%DEST%" (
    mkdir "%DEST%"
    if errorlevel 1 (
        echo [ERREUR] Impossible de creer %DEST% ^(acces refuse ou partage indisponible^).
        goto :fail
    )
)

REM --- Step 3: copy only what is needed to RUN the tool -----------------------
echo.
echo [3/4] Copie des fichiers necessaires...
copy /Y "%PUBLISH_DIR%\gccphat.exe" "%DEST%\gccphat.exe" >nul
if errorlevel 1 (
    echo [ERREUR] Echec de la copie de gccphat.exe.
    goto :fail
)
echo   - gccphat.exe

if exist "%SAMPLE%" (
    copy /Y "%SAMPLE%" "%DEST%\stereo_noise.wav" >nul
    echo   - stereo_noise.wav ^(fichier d'exemple pour tester^)
)

REM --- Generate a short usage note for the team -------------------------------
set "README=%DEST%\README.txt"
>  "%README%" echo GCC-PHAT - Time delay estimation between stereo channels
>> "%README%" echo ============================================================
>> "%README%" echo.
>> "%README%" echo No installation required: gccphat.exe is fully standalone.
>> "%README%" echo.
>> "%README%" echo Usage:
>> "%README%" echo   gccphat.exe ^<audioFilePath^> ^<bufferSize^> ^<fmin^> ^<fmax^> ^<outputMode^>
>> "%README%" echo.
>> "%README%" echo   audioFilePath : stereo WAV file
>> "%README%" echo   bufferSize    : window size (power of two: 1024, 2048, 4096...)
>> "%README%" echo   fmin / fmax   : frequency band in Hz (band-pass filter)
>> "%README%" echo   outputMode    : console (print) or csv (CSV file)
>> "%README%" echo.
>> "%README%" echo Example:
>> "%README%" echo   gccphat.exe stereo_noise.wav 4096 200 8000 console
>> "%README%" echo.
>> "%README%" echo Sign convention:
>> "%README%" echo   negative delay = channel 2 is delayed relative to channel 1
>> "%README%" echo   positive delay = channel 1 is delayed relative to channel 2
>> "%README%" echo.
>> "%README%" echo Documentation: https://github.com/pmarmaroli/gccphat
echo   - README.txt

REM --- Step 4: done -----------------------------------------------------------
echo.
echo [4/4] Termine.
echo ============================================================
echo  Deploiement reussi vers : %DEST%
echo ============================================================
goto :end

:fail
echo.
echo Deploiement INTERROMPU.
endlocal
exit /b 1

:end
endlocal
exit /b 0
